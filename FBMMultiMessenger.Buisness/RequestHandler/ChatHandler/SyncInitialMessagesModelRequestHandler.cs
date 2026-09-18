using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class SyncInitialMessagesModelRequestHandler : IRequestHandler<SyncInitialMessagesModelRequest, BaseResponse<SyncInitialMessagesModelResponse>>
    {
        private readonly ApplicationDbContext dbContext;
        private readonly CurrentUserService currentUserService;
        private readonly OneSignalService oneSignalService;

        public SyncInitialMessagesModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, OneSignalService oneSignalService)
        {
            this.dbContext = dbContext;
            this.currentUserService = currentUserService;
            this.oneSignalService = oneSignalService;
        }

        public async Task<BaseResponse<SyncInitialMessagesModelResponse>> Handle(SyncInitialMessagesModelRequest request, CancellationToken cancellationToken)
        {
            var retriedCount = 0;

            while(true)
                {
                    try
                    {
                        var currentUser = currentUserService.GetCurrentUser();

                        //Extra safety check: If the user has came to this point he will be logged in hence currentuser will never be null.
                        if (currentUser is null)
                        {
                            return BaseResponse<SyncInitialMessagesModelResponse>.Error("Invalid Request, Please login again to continue");
                        }

                        var account = await dbContext
                            .Accounts
                            .Include(a => a.Chats)
                            .ThenInclude(c => c.ChatMessages)
                            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

                        if (account is null)
                        {
                            return BaseResponse<SyncInitialMessagesModelResponse>.Error("Account not found");
                        }

                        // Get all existing message IDs for this account to avoid duplicate queries
                        var existingMessageIds = account.Chats
                            .SelectMany(c => c.ChatMessages)
                            .Select(m => m.FbMessageId)
                            .ToHashSet();

                        int newMessagesCount = 0;

                        var anyChangeMade = false;

                        // Chats that got newly-synced unread received messages → notify (same rules as HandleChatModeRequestHandler).
                        var chatsWithNewUnread = new HashSet<FBMMultiMessenger.Data.Database.DbModels.Chat>();

                        foreach (var syncChat in request.Chats)
                        {
                            // Find or create chat
                            var chat = account.Chats.FirstOrDefault(c => c.FBChatId == syncChat.FbChatId);

                            if (chat is null)
                            {
                                anyChangeMade = true;

                                chat = new FBMMultiMessenger.Data.Database.DbModels.Chat
                                {
                                    FBChatId = syncChat.FbChatId,
                                    FbAccountId = request.FbAccountId,
                                    OtherUserId = syncChat.OtherUserId,
                                    OtherUserName = syncChat.OtherUserName,
                                    UserProfileImage = syncChat.OtherUserProfilePicture,
                                    FbListingTitle = syncChat.ListingTitle,
                                    FBListingImage = syncChat.ListingImage,
                                    FbListingId = syncChat.FbListingId,
                                    FbListingLocation = syncChat.FbListingLocation,
                                    FbListingPrice = syncChat.FbListingPrice,
                                    IsRead = syncChat.IsRead,
                                    AccountId = account.Id,
                                    UserId = currentUser.Id,
                                    StartedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow,
                                    ChatMessages = new List<FBMMultiMessenger.Data.Database.DbModels.ChatMessages>()
                                };
                                account.Chats.Add(chat);
                            }
                            else
                            {
                                // Update chat details if needed
                                if (string.IsNullOrWhiteSpace(chat.OtherUserId) && !string.IsNullOrWhiteSpace(syncChat.OtherUserId))
                                {
                                    chat.OtherUserId = syncChat.OtherUserId;
                                    anyChangeMade = true;
                                }

                                if (string.IsNullOrWhiteSpace(chat.OtherUserName) && !string.IsNullOrWhiteSpace(syncChat.OtherUserName))
                                {
                                    chat.OtherUserName = syncChat.OtherUserName;
                                    anyChangeMade = true;
                                }

                                if (string.IsNullOrWhiteSpace(chat.UserProfileImage) && !string.IsNullOrWhiteSpace(syncChat.OtherUserProfilePicture))
                                {
                                    chat.UserProfileImage = syncChat.OtherUserProfilePicture;
                                    anyChangeMade = true;
                                }

                                if (string.IsNullOrWhiteSpace(chat.FbListingTitle) && !string.IsNullOrWhiteSpace(syncChat.ListingTitle))
                                {
                                    chat.FbListingTitle = syncChat.ListingTitle;
                                    anyChangeMade = true;
                                }

                                if (string.IsNullOrWhiteSpace(chat.FBListingImage) && !string.IsNullOrWhiteSpace(syncChat.ListingImage))
                                {
                                    chat.FBListingImage = syncChat.ListingImage;
                                    anyChangeMade = true;
                                }

                                if (string.IsNullOrWhiteSpace(chat.FbListingId) && !string.IsNullOrWhiteSpace(syncChat.FbListingId))
                                {
                                    chat.FbListingId = syncChat.FbListingId;
                                    anyChangeMade = true;
                                }

                                if (string.IsNullOrWhiteSpace(chat.FbListingLocation) && !string.IsNullOrWhiteSpace(syncChat.FbListingLocation))
                                {
                                    chat.FbListingLocation = syncChat.FbListingLocation;
                                    anyChangeMade = true;
                                }

                                if (chat.FbListingPrice == null && syncChat.FbListingPrice != null)
                                {
                                    chat.FbListingPrice = syncChat.FbListingPrice;
                                    anyChangeMade = true;
                                }

                                // Surface unread from the sync, but never clear a read the client
                                // already set locally (client owns marking chats read once opened).
                                if (!syncChat.IsRead && chat.IsRead)
                                {
                                    chat.IsRead = false;
                                    anyChangeMade = true;
                                }

                                if (anyChangeMade)
                                {
                                    chat.UpdatedAt = DateTime.UtcNow;
                                }
                            }

                            // Sync messages - only add new ones
                            foreach (var syncMessage in syncChat.Messages)
                            {
                                if (!existingMessageIds.Contains(syncMessage.MessageId))
                                {
                                    syncMessage.IsTextMessage = !string.IsNullOrWhiteSpace(syncMessage.Text);
                                    syncMessage.IsImageMessage = !syncMessage.IsTextMessage;
                                    //ToDO
                                    syncMessage.IsVideoMessage = false;
                                    syncMessage.IsAudioMessage = false;

                                    var dbMessage = string.Empty;

                                    if (syncMessage.IsImageMessage || syncMessage.IsVideoMessage)
                                    {
                                        dbMessage = JsonSerializer.Serialize(syncMessage.Attachments);
                                    }
                                    else
                                    {
                                        dbMessage = syncMessage.Text;
                                    }

                                    var chatMessage = new FBMMultiMessenger.Data.Database.DbModels.ChatMessages
                                    {
                                        FbMessageId = syncMessage.MessageId,
                                        FbMessageReplyId = syncMessage.FbMessageReplyId,
                                        FBTimestamp = syncMessage.Timestamp,
                                        Message = dbMessage,
                                        IsReceived = syncMessage.IsReceived,
                                        IsSent = true,
                                        IsRead = syncMessage.IsRead,
                                        IsTextMessage = syncMessage.IsTextMessage,
                                        IsImageMessage = syncMessage.IsImageMessage,
                                        IsVideoMessage = syncMessage.IsVideoMessage,
                                        IsAudioMessage = syncMessage.IsAudioMessage,
                                        ChatId = chat.Id,
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow,
                                    };

                                    chat.ChatMessages.Add(chatMessage);
                                    chat.UpdatedAt = DateTime.UtcNow;
                                    existingMessageIds.Add(syncMessage.MessageId);
                                    newMessagesCount++;
                                    anyChangeMade = true;

                                    // Only received messages that are still unread trigger a notification.
                                    if (chatMessage.IsReceived && !chatMessage.IsRead)
                                    {
                                        chatsWithNewUnread.Add(chat);
                                    }
                                }
                            }
                        }

                        if (anyChangeMade || newMessagesCount > 0)
                        {
                            await dbContext.SaveChangesAsync(cancellationToken);
                        }

                        // Notify for newly-synced unread messages. Wrapped so a notification failure
                        // never fails the sync (messages are already persisted).
                        if (chatsWithNewUnread.Count > 0)
                        {
                            try
                            {
                                await SendUnreadNotificationsAsync(chatsWithNewUnread, currentUser.Id);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        return BaseResponse<SyncInitialMessagesModelResponse>.Success(
                            $"Successfully synced {newMessagesCount} new message(s)",
                            new SyncInitialMessagesModelResponse()
                        );
                    }
                    catch (DbUpdateException ex)
                    {
                        if (retriedCount >= 2)
                        {
                            return BaseResponse<SyncInitialMessagesModelResponse>.Error("An error occurred."); // second failure → return failure
                        }

                        retriedCount++;

                        // 🔹 Clear tracked entities (important!)
                        dbContext.ChangeTracker.Clear();

                        // 🔹 Small delay to allow other transaction to commit
                        await Task.Delay(3000, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        return BaseResponse<SyncInitialMessagesModelResponse>.Error("An error occurred.");
                    }
                }
        }

        // Sends one aggregated push notification per chat for its unread received messages,
        // following the same rules as HandleChatModeRequestHandler: notifications require an
        // active subscription, and only received/unread messages are surfaced.
        private async Task SendUnreadNotificationsAsync(IEnumerable<FBMMultiMessenger.Data.Database.DbModels.Chat> chats, int userId)
        {
            var today = DateTime.UtcNow;

            var activeSubscription = dbContext
                .Subscriptions
                .AsNoTracking()
                .Where(x => x.StartedAt <= today && x.ExpiredAt > today && x.UserId == userId)
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefault();

            // No active subscription (expired or none): still notify, but without message content —
            // prompt the user to renew (mirrors the expired branch in HandleChatModeRequestHandler).
            if (activeSubscription is null)
            {
                try
                {
                    await oneSignalService.SendMessageNotification(
                        userId: userId.ToString(),
                        message: "New messages waiting! Renew your subscription to view them.",
                        senderName: "FBM Multi Messenger",
                        chatId: 0
                    );
                }
                catch (Exception)
                {
                }

                return;
            }

            foreach (var chat in chats)
            {
                var unreadMessages = chat.ChatMessages
                    .Where(m => m.IsReceived && !m.IsRead)
                    .OrderBy(m => m.FBTimestamp)
                    .ToList();

                if (unreadMessages.Count == 0)
                {
                    continue;
                }

                var message = string.Join("\n", unreadMessages.Select(m => m switch
                {
                    { IsImageMessage: true } => "You have received an image",
                    { IsVideoMessage: true } => "You have received a video",
                    { IsAudioMessage: true } => "You have received an audio",
                    _ => m.Message
                }));

                var senderName = string.IsNullOrWhiteSpace(chat.FbListingTitle) ? "FBM Multi Messenger" : chat.FbListingTitle;

                try
                {
                    await oneSignalService.SendMessageNotification(
                        userId: userId.ToString(),
                        message: message,
                        senderName: senderName,
                        chatId: chat.Id
                    );
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
