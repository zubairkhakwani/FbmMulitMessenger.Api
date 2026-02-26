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
                                       .Include(c => c.Account)
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
                    UpdatedAt = today,

                };

                await _dbContext.AddAsync(newChat, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                chatReference = newChat;
            }

            else if (chatReference is not null)
            {
                // Update chat details if needed
                if (string.IsNullOrWhiteSpace(chatReference.OtherUserId) && !string.IsNullOrWhiteSpace(request.OtherUserId))
                {
                    chatReference.OtherUserId = request.OtherUserId;
                }

                if (string.IsNullOrWhiteSpace(chatReference.OtherUserName) && !string.IsNullOrWhiteSpace(request.OtherUserName))
                {
                    chatReference.OtherUserName = request.OtherUserName;
                }

                if (string.IsNullOrWhiteSpace(chatReference.UserProfileImage) && !string.IsNullOrWhiteSpace(request.OtherUserProfilePicture))
                {
                    chatReference.UserProfileImage = request.OtherUserProfilePicture;
                }

                if (string.IsNullOrWhiteSpace(chatReference.UserProfileImage) && !string.IsNullOrWhiteSpace(request.UserProfileImg))
                {
                    chatReference.UserProfileImage = request.UserProfileImg;
                }
                if (string.IsNullOrWhiteSpace(chatReference.FbListingTitle) && !string.IsNullOrWhiteSpace(request.FbListingTitle))
                {
                    chatReference.FbListingTitle = request.FbListingTitle;
                }

                if (string.IsNullOrWhiteSpace(chatReference.FBListingImage) && !string.IsNullOrWhiteSpace(request.FbListingImg))
                {
                    chatReference.FBListingImage = request.FbListingImg;
                }

                if (string.IsNullOrWhiteSpace(chatReference.FbListingId) && !string.IsNullOrWhiteSpace(request.FbListingId))
                {
                    chatReference.FbListingId = request.FbListingId;
                }

                if (string.IsNullOrWhiteSpace(chatReference.FbListingLocation) && !string.IsNullOrWhiteSpace(request.FbListingLocation))
                {
                    chatReference.FbListingLocation = request.FbListingLocation;
                }

                if (chat.FbListingPrice == null && request.FbListingPrice != null)
                {
                    chat.FbListingPrice = request.FbListingPrice;
                }

                chat.UpdatedAt = DateTime.UtcNow;
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
                UpdatedAt = DateTime.UtcNow,
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
                await SendMessageToAppAsync(request, chatReference!, newChatMessage.Id, newChatMessage.CreatedAt, newChatMessage.FBTimestamp, dbMessage, cancellationToken);
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
                    chatId: chatId
                );
            }
            catch (Exception ex)
            {

            }
        }

        private async Task SendMessageToAppAsync(HandleChatModelRequest request, Chat chat, int chatMessageId, DateTime CreatedAt, long? fbTimeStamp, string message, CancellationToken cancellationToken)
        {
            var chatMessage = await _dbContext.ChatMessages
                                              .Include(cm => cm.Chat)
                                              .AsNoTracking()
                                              .FirstOrDefaultAsync(cm => cm.FbMessageId == request.FbMessageReplyId, cancellationToken)
                                              ??
                                              new ChatMessages();

            var replyResult = ChatMessagesHelper.GetMessageReply(new MessageReplyRequest() { ChatMessages = [chatMessage], FbMessageReplyId= request.FbMessageReplyId });

            var messageReply = replyResult is null ? null : new MessageReplyHttpResponse()
            {
                Type = replyResult.Type,
                Reply = replyResult.Reply,
                ReplyTo = replyResult.ReplyTo,
                Attachments = replyResult.Attachments?.Select(x => new MessageReplyFileHttpResponse()
                {
                    Url  = x.Url
                }).ToList()
            };

            var fbListingId = request.FbListingId ?? chat.FbListingId;

            //Inform the client via signalR.
            var receivedChat = new HandleChatHttpResponse()
            {
                Message = message,
                ChatId = chat.Id,
                ChatMessageId = chatMessageId,
                FbMessageId = request.FbMessageId,
                FbMessageReplyId = request.FbMessageReplyId,
                FbChatId = request.FbChatId,
                FbAccountId = request.FbAccountId,
                FbListingId = fbListingId,
                FbListingTitle = chat.FbListingTitle,
                FbListingLocation = chat.FbListingLocation,
                FbListingPrice = chat.FbListingPrice,
                FbListingImage = chat.FBListingImage,
                OfflineUniqueId = request.OfflineUniqueId,
                UserProfileImage = chat.UserProfileImage,
                IsTextMessage = request.IsTextMessage,
                IsVideoMessage = request.IsVideoMessage,
                IsImageMessage = request.IsImageMessage,
                IsAudioMessage = request.IsAudioMessage,
                IsReceived = request.IsReceived,
                MessageReply = messageReply,
                CreatedAt = CreatedAt,
                FbTimestamp = fbTimeStamp,
                AccountId = request.AccountId,
                AccountName = chat?.Account?.Name
            };

            var result = ChatMessagesHelper.GetMessagePreview(request.ToMessagePreviewRequest(chat.OtherUserName));

            receivedChat.MessagPreview = result.MessagPreview;
            receivedChat.MessagePreviewFrom = result.SenderName;


            var sendMessageToUserId = $"App_{chat.UserId}";

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
