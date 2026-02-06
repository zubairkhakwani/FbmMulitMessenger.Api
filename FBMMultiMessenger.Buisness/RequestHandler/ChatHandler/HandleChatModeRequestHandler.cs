using FBMMultiMessenger.Buisness.Exntesions;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Shared;
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
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ChatHub _chatHub;
        private readonly OneSignalService _oneSignalNotificationService;

        private static readonly ConcurrentDictionary<string, DateTime> _processedOTIDs = new ConcurrentDictionary<string, DateTime>();
        private static DateTime _lastCleanup = DateTime.UtcNow;

        public HandleChatModeRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> hubContext, ChatHub chatHub, OneSignalService oneSignalNotificationService)
        {
            this._dbContext = dbContext;
            this._currentUserService=currentUserService;
            this._hubContext = hubContext;
            this._chatHub = chatHub;
            this._oneSignalNotificationService = oneSignalNotificationService;
        }
        public async Task<BaseResponse<HandleChatModelResponse>> Handle(HandleChatModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currentuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<HandleChatModelResponse>.Error("Invalid Request, Please login again to continue");
            }

            if (request.FbOTID != null)
            {
                string compositeKey = $"{request.FbAccountId}_{request.FbChatId}_{request.FbOTID}_{request.AccountId}_{currentUser.Id}";
                var now = DateTime.UtcNow;

                // Try to add with timestamp
                if (!_processedOTIDs.TryAdd(compositeKey, now))
                {
                    return BaseResponse<HandleChatModelResponse>.Success($"Duplicate message ignored.", new HandleChatModelResponse());
                }

                // Clean up old entries every 3 minutes (non-blocking)
                if ((now - _lastCleanup).TotalMinutes >= 3)
                {
                    _lastCleanup = now;
                    Task.Run(() => CleanupOldOTIDs(now));
                }
            }

            var chat = await _dbContext.Chats
                                       .FirstOrDefaultAsync(x => x.AccountId == request.AccountId && x.FBChatId == request.FbChatId && x.UserId == currentUser.Id, cancellationToken);

            var chatReference = chat;
            var today = DateTime.UtcNow;
            if (chat is null)
            {
                var newChat = new Chat()
                {
                    UserId = currentUser.Id,
                    AccountId = request.AccountId,
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

            else if (chatReference is not null)
            {
                var userProfileImg = chatReference.UserProfileImage;
                var fbListingTitle = chatReference.FbListingTitle;
                var fbListingImage = chatReference.FBListingImage;

                if (request.UserProfileImg is not null && userProfileImg is null)
                {
                    chatReference.UserProfileImage = request.UserProfileImg;
                }
                if (request.FbListingTitle is not null && fbListingTitle is null)
                {
                    chatReference.FbListingTitle = request.FbListingTitle;
                }

                if (request.FbListingImg is not null && fbListingImage is null)
                {
                    chatReference.FBListingImage = request.FbListingImg;
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
                FbMessageId = request.FbMessageId,
                FbMessageReplyId = request.FbMessageReplyId,
                FBTimestamp = request.Timestamp,
            };

            await _dbContext.ChatMessages.AddAsync(newChatMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);



            //Get active subscription of the current user.
            var activeSubscription = _dbContext
                                     .Subscriptions
                                     .AsNoTracking()
                                     .Where(x => x.StartedAt <= today
                                            &&
                                     x.ExpiredAt > today
                                            &&
                                     x.UserId == currentUser.Id)
                                    .OrderByDescending(x => x.StartedAt)
                                    .FirstOrDefault();


            if (activeSubscription is null)
            {
                return BaseResponse<HandleChatModelResponse>.Success($"Message received, but the user does not have any active subscription. No notification was sent and the UI was not updated.", new HandleChatModelResponse());
            }

            var endDate = activeSubscription.ExpiredAt;
            var isSubscriptionExpired = today >= endDate;

            if (!isSubscriptionExpired)
            {
                await SendMessageToAppAsync(request, chatReference!, newChatMessage.Id, newChatMessage.CreatedAt, dbMessage, cancellationToken);
            }

            if (request.IsReceived)
            {
                var messageFrom = chatReference.FbListingTitle;

                var unreadMessages = _dbContext.ChatMessages.Include(c => c.Chat)
                    .Where(cm => cm.Chat.FBChatId == request.FbChatId && !cm.IsRead)
                    .OrderBy(cm => cm.FBTimestamp)
                    .ToList();

                await SendMobileNotificationAsync(request, chatReference.Id, unreadMessages, chatReference!.UserId, messageFrom, isSubscriptionExpired);
            }

            var responseMessage = isSubscriptionExpired ? "Message received, but the user's subscription has expired." : "Message has been received successfully";
            return BaseResponse<HandleChatModelResponse>.Success(responseMessage, new HandleChatModelResponse());
        }
        private async Task SendMobileNotificationAsync(HandleChatModelRequest request, int chatId, List<ChatMessages> messages, int userId, string? messageFrom, bool isSubscriptionExpired = false)
        {
            try
            {
                string message = string.Empty;

                if (!isSubscriptionExpired)
                {
                    var count = request.Messages.Count;

                    var tempMessage = messages.Select(m =>
                    {
                        string receivedMessage = m switch
                        {
                            { IsImageMessage: true } => $"You have received {count} images",
                            { IsVideoMessage: true } => $"You have received {count} videos",
                            { IsAudioMessage: true } => $"You have received {count} audio",
                            _ => m.Message
                        };

                        return receivedMessage;
                    }).ToList();

                    message = string.Join("\n", tempMessage);
                }
                else
                {
                    message = "New messages waiting! Renew your subscription to view them.";
                    request.FbChatId = string.Empty;
                }

                await _oneSignalNotificationService.SendMessageNotification(
                    userId: userId.ToString(),
                    message: message,
                    senderName: messageFrom ?? "FBM Multi Messenger",
                    chatId: chatId,
                    isSubscriptionExpired: isSubscriptionExpired
                );
            }
            catch (Exception ex)
            {

            }
        }

        private async Task SendMessageToAppAsync(HandleChatModelRequest request, Chat chat, int chatMessageId, DateTime CreatedAt, string message, CancellationToken cancellationToken)
        {
            var sendMessageToUserId = $"App_{chat.UserId}";

            var chatMessage = await _dbContext.ChatMessages
                                              .FirstOrDefaultAsync(cm => cm.FbMessageId == request.FbMessageReplyId, cancellationToken)
                                              ??
                                              new ChatMessages();

            //Inform the client via signalR.
            var receivedChat = new HandleChatHttpResponse()
            {
                Message = message,
                ChatId = chat.Id,
                ChatMessageId = chatMessageId,
                FbMessageReplyId = request.FbMessageReplyId,
                MessageReply = ChatMessagesHelper.GetMessageReply(new MessageReplyRequest() { ChatMessages = [chatMessage], FbMessageReplyId= request.FbMessageReplyId })?.Message,
                MessageReplyTo = ChatMessagesHelper.GetMessageReply(new MessageReplyRequest() { ChatMessages = [chatMessage], FbMessageReplyId= request.FbMessageReplyId })?.ReplyTo,
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

            var result = ChatMessagesHelper.GetMessagePreview(request.ToMessagePreviewRequest());

            receivedChat.MessagPreview = result.MessagPreview;
            receivedChat.MessagePreviewFrom = result.SenderName;


            await _hubContext.Clients.Group(sendMessageToUserId)
                .SendAsync("HandleMessage", receivedChat, cancellationToken);
        }

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
