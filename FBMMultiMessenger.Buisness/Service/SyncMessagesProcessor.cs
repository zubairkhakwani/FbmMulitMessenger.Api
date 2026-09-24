using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Data.DB;
using Microsoft.EntityFrameworkCore;
using Sentry;
using System.Text.Json;
using DbModels = FBMMultiMessenger.Data.Database.DbModels;

namespace FBMMultiMessenger.Buisness.Service
{
    /// <summary>
    /// Persists a batch of history-sync chats/messages for one account. Extracted from the old
    /// SyncInitialMessagesModelRequestHandler so it can be driven by the batched SyncFlushService
    /// (no HTTP context — the user id is passed in). Dedupes against existing messages.
    /// </summary>
    public class SyncMessagesProcessor
    {
        private readonly ApplicationDbContext dbContext;
        private readonly OneSignalService oneSignalService;

        public SyncMessagesProcessor(ApplicationDbContext dbContext, OneSignalService oneSignalService)
        {
            this.dbContext = dbContext;
            this.oneSignalService = oneSignalService;
        }

        public async Task ProcessAsync(int userId, int accountId, string fbAccountId, List<SyncChatsModel> chats, CancellationToken cancellationToken)
        {
            var retriedCount = 0;

            while (true)
            {
                try
                {
                    var account = await dbContext
                        .Accounts
                        .Include(a => a.Chats)
                        .ThenInclude(c => c.ChatMessages)
                        .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

                    if (account is null)
                    {
                        return;
                    }

                    // Get all existing message IDs for this account to avoid duplicate queries
                    var existingMessageIds = account.Chats
                        .SelectMany(c => c.ChatMessages)
                        .Select(m => m.FbMessageId)
                        .ToHashSet();

                    int newMessagesCount = 0;

                    var anyChangeMade = false;

                    // Chats that got newly-synced unread received messages → notify (same rules as HandleChatModeRequestHandler).
                    var chatsWithNewUnread = new HashSet<DbModels.Chat>();

                    foreach (var syncChat in chats)
                    {
                        // Tracks whether THIS chat changed, so we only bump its UpdatedAt when it actually did.
                        var chatChanged = false;

                        // Find or create chat
                        var chat = account.Chats.FirstOrDefault(c => c.FBChatId == syncChat.FbChatId);

                        if (chat is null)
                        {
                            chatChanged = true;

                            chat = new DbModels.Chat
                            {
                                FBChatId = syncChat.FbChatId,
                                FbAccountId = fbAccountId,
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
                                UserId = userId,
                                StartedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                ChatMessages = new List<DbModels.ChatMessages>()
                            };
                            account.Chats.Add(chat);
                        }
                        else
                        {
                            // Update chat details if needed
                            if (string.IsNullOrWhiteSpace(chat.OtherUserId) && !string.IsNullOrWhiteSpace(syncChat.OtherUserId))
                            {
                                chat.OtherUserId = syncChat.OtherUserId;
                                chatChanged = true;
                            }

                            if (string.IsNullOrWhiteSpace(chat.OtherUserName) && !string.IsNullOrWhiteSpace(syncChat.OtherUserName))
                            {
                                chat.OtherUserName = syncChat.OtherUserName;
                                chatChanged = true;
                            }

                            if (string.IsNullOrWhiteSpace(chat.UserProfileImage) && !string.IsNullOrWhiteSpace(syncChat.OtherUserProfilePicture))
                            {
                                chat.UserProfileImage = syncChat.OtherUserProfilePicture;
                                chatChanged = true;
                            }

                            if (string.IsNullOrWhiteSpace(chat.FbListingTitle) && !string.IsNullOrWhiteSpace(syncChat.ListingTitle))
                            {
                                chat.FbListingTitle = syncChat.ListingTitle;
                                chatChanged = true;
                            }

                            if (string.IsNullOrWhiteSpace(chat.FBListingImage) && !string.IsNullOrWhiteSpace(syncChat.ListingImage))
                            {
                                chat.FBListingImage = syncChat.ListingImage;
                                chatChanged = true;
                            }

                            if (string.IsNullOrWhiteSpace(chat.FbListingId) && !string.IsNullOrWhiteSpace(syncChat.FbListingId))
                            {
                                chat.FbListingId = syncChat.FbListingId;
                                chatChanged = true;
                            }

                            if (string.IsNullOrWhiteSpace(chat.FbListingLocation) && !string.IsNullOrWhiteSpace(syncChat.FbListingLocation))
                            {
                                chat.FbListingLocation = syncChat.FbListingLocation;
                                chatChanged = true;
                            }

                            if (chat.FbListingPrice == null && syncChat.FbListingPrice != null)
                            {
                                chat.FbListingPrice = syncChat.FbListingPrice;
                                chatChanged = true;
                            }

                            // Surface unread from the sync, but never clear a read the client
                            // already set locally (client owns marking chats read once opened).
                            if (!syncChat.IsRead && chat.IsRead)
                            {
                                chat.IsRead = false;
                                chatChanged = true;
                            }

                            if (chatChanged)
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

                                var chatMessage = new DbModels.ChatMessages
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
                                chatChanged = true;

                                // Only received messages that are still unread trigger a notification.
                                if (chatMessage.IsReceived && !chatMessage.IsRead)
                                {
                                    chatsWithNewUnread.Add(chat);
                                }
                            }
                        }

                        // Roll this chat's change into the batch-wide flag used for the SaveChanges decision.
                        anyChangeMade |= chatChanged;
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
                            await SendUnreadNotificationsAsync(chatsWithNewUnread, userId);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    return;
                }
                catch (DbUpdateException ex)
                {
                    if (retriedCount >= 2)
                    {
                        SentrySdk.CaptureException(ex, scope => scope.SetTag("accountId", accountId.ToString()));
                        return;
                    }

                    retriedCount++;
                    dbContext.ChangeTracker.Clear();
                    await Task.Delay(3000, cancellationToken);
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex, scope => scope.SetTag("accountId", accountId.ToString()));
                    return;
                }
            }
        }

        // Sends one aggregated push notification per chat for its unread received messages,
        // following the same rules as HandleChatModeRequestHandler: notifications require an
        // active subscription, and only received/unread messages are surfaced.
        private async Task SendUnreadNotificationsAsync(IEnumerable<DbModels.Chat> chats, int userId)
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
