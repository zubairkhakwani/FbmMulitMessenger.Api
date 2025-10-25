using FBMMultiMessenger.Buisness.Notifaciton;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class HandleChatModeRequestHandler : IRequestHandler<HandleChatModelRequest, BaseResponse<HandleChatModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ChatHub _chatHub;
        private readonly OneSignalService _oneSignalNotificationService;

        private static readonly ConcurrentDictionary<string, DateTime> _processedOTIDs = new ConcurrentDictionary<string, DateTime>();
        private static DateTime _lastCleanup = DateTime.UtcNow;

        public HandleChatModeRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext, ChatHub chatHub, OneSignalService oneSignalNotificationService)
        {
            this._dbContext = dbContext;
            this._hubContext = hubContext;
            this._chatHub = chatHub;
            this._oneSignalNotificationService = oneSignalNotificationService;
        }
        public async Task<BaseResponse<HandleChatModelResponse>> Handle(HandleChatModelRequest request, CancellationToken cancellationToken)
        {
            if (request.FbOTID != null)
            {
                string compositeKey = $"{request.FbAccountId}_{request.FbChatId}_{request.FbOTID}";
                var now = DateTime.UtcNow;

                // Try to add with timestamp
                if (!_processedOTIDs.TryAdd(compositeKey, now))
                {
                    return BaseResponse<HandleChatModelResponse>.Success($"Duplicate message ignored.",new HandleChatModelResponse());
                }

                // Clean up old entries every 3 minutes (non-blocking)
                if ((now - _lastCleanup).TotalMinutes >= 3)
                {
                    _lastCleanup = now;
                    Task.Run(() => CleanupOldOTIDs(now));
                }
            }


            var chat = await _dbContext.Chats
                                       .FirstOrDefaultAsync(x => x.FBChatId == request.FbChatId, cancellationToken);

            var chatReference = chat;
            var today = DateTime.UtcNow;
            if (chat is null)
            {
                var newChat = new Chat()
                {
                    UserId = request.UserId,
                    FBChatId = request.FbChatId,
                    FbAccountId = request.FbAccountId,
                    FbListingId = request.FbListingId,
                    FbListingTitle = request.FbListingTitle,
                    FBListingImage = request.FbListingImg,
                    UserProfileImage = request.UserProfileImg,
                    FbListingLocation = request.FbListingLocation,
                    FbListingPrice = request.FbListingPrice,
                    IsRead = !request.IsReceived,
                    StartedAt = today,
                    UpdatedAt = today
                };

                await _dbContext.AddAsync(newChat, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                chatReference = newChat;
            }

            var userProfileImg = chatReference?.UserProfileImage;
            if (chatReference is not null)
            {
                if (request.UserProfileImg is not null && userProfileImg is null)
                {
                    chatReference.UserProfileImage = request.UserProfileImg;
                }
                chatReference.UpdatedAt = today;
                chatReference.IsRead = !request.IsReceived;
            }

            var dbMessage = string.Empty;

            if (request.IsImageMessage || request.IsVideoMessage)
            {
                dbMessage = JsonSerializer.Serialize(request.Messages);
            }
            else
            {

                dbMessage = request.Messages.FirstOrDefault() ?? string.Empty;
            }

            var newChatMessage = new ChatMessages()
            {
                Message = dbMessage.Trim(),
                ChatId = chatReference!.Id,
                IsReceived = request.IsReceived,
                IsRead = !request.IsReceived,
                IsSent = true,
                IsTextMessage = request.IsTextMessage,
                IsVideoMessage = request.IsVideoMessage,
                IsImageMessage = request.IsImageMessage,
                IsAudioMessage = request.IsAudioMessage,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.ChatMessages.AddAsync(newChatMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);



            //Get active subscription of the current user.
            var activeSubscription = _dbContext.Subscriptions
                                     .Where(x => x.StartedAt <= today
                                            &&
                                     x.ExpiredAt > today
                                            &&
                                     x.UserId == request.UserId)
                                    .OrderByDescending(x => x.StartedAt)
                                    .FirstOrDefault();



            if (activeSubscription is null)
            {
                return BaseResponse<HandleChatModelResponse>.Success($"Message received, but the user does not have any active subscription. No notification was sent and the UI was not updated.", new HandleChatModelResponse());
            }

            var endDate = activeSubscription.ExpiredAt;
            var isSubscriptionExpired = today >= endDate;

            if (request.IsReceived)
            {
                await SendMobileNotificationAsync(request, chatReference!.UserId, isSubscriptionExpired);
            }

            if (!isSubscriptionExpired)
            {
                await SendMessageToAppAsync(request, chatReference!, newChatMessage.CreatedAt, dbMessage, cancellationToken);
            }


            var responseMessage = isSubscriptionExpired ? "Message received, but the user's subscription has expired." : "Message has been received successfully";
            return BaseResponse<HandleChatModelResponse>.Success(responseMessage, new HandleChatModelResponse());
        }
        private async Task SendMobileNotificationAsync(HandleChatModelRequest request, int userId, bool isSubscriptionExpired = false)
        {
            string message = string.Empty;

            if (!isSubscriptionExpired)
            {
                var count = request.Messages.Count;

                string receivedMessage = request switch
                {
                    { IsImageMessage: true } => $"You have received {count} images",
                    { IsVideoMessage: true } => $"You have received {count} videos",
                    { IsAudioMessage: true } => $"You have received {count} videos",
                    _ => request.Messages.FirstOrDefault()!
                };
                message = receivedMessage;
            }
            else
            {
                message = "New messages waiting! Renew your subscription to view them.";
                request.FbChatId = string.Empty;
            }

            //var devices = ChatHub._devices;
            //var device = devices.FirstOrDefault(x => x.Key == request.FbChatId);


            await _oneSignalNotificationService.SendMessageNotification(
                userId: userId.ToString(),
                message: message,
                senderName: "Fbm Multi Messenger",
                chatId: request.FbChatId,
                isSubscriptionExpired: isSubscriptionExpired
            );
        }

        private async Task SendMessageToAppAsync(HandleChatModelRequest request, Chat chat, DateTime CreatedAt, string message, CancellationToken cancellationToken)
        {
            var sendMessageToUserId = $"User_{chat.UserId}";

            //Inform the client via signalR.
            var receivedChat = new HandleChatHttpResponse()
            {
                Message = message,
                ChatId = chat.Id,
                FbChatId = request.FbChatId,
                FbAccountId = request.FbAccountId,
                FbListingId = request.FbListingId!,
                FbListingTitle = chat.FbListingTitle,
                FbListingLocation = chat.FbListingLocation,
                FbListingPrice = chat.FbListingPrice,
                FbListingImage = chat.FBListingImage,
                OfflineUniqueId =  request.OfflineUniqueId,
                UserProfileImage = chat.UserProfileImage,
                IsTextMessage = request.IsTextMessage,
                IsVideoMessage =request.IsVideoMessage,
                IsImageMessage = request.IsImageMessage,
                IsAudioMessage = request.IsAudioMessage,
                IsReceived = request.IsReceived,
                StartedAt = CreatedAt,
            };

            await _hubContext.Clients.Group(sendMessageToUserId)
                .SendAsync("HandleMessage", receivedChat, cancellationToken);
        }

        // Add this new method to the class
        private void CleanupOldOTIDs(DateTime currentTime)
        {
            var keysToRemove = _processedOTIDs
                .Where(kvp => (currentTime - kvp.Value).TotalMinutes > 3)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _processedOTIDs.TryRemove(key, out _);
            }
        }
    }
}
