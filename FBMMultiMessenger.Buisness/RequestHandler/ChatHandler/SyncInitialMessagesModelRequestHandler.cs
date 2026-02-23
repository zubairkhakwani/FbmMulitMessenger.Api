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

        public SyncInitialMessagesModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this.dbContext = dbContext;
            this.currentUserService = currentUserService;
        }

        public async Task<BaseResponse<SyncInitialMessagesModelResponse>> Handle(SyncInitialMessagesModelRequest request, CancellationToken cancellationToken)
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

                        if(anyChangeMade)
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
                                IsRead = true,
                                IsTextMessage = syncMessage.IsTextMessage,
                                IsImageMessage = syncMessage.IsImageMessage,
                                IsVideoMessage = syncMessage.IsVideoMessage,
                                IsAudioMessage = syncMessage.IsAudioMessage,
                                ChatId = chat.Id,
                                CreatedAt = DateTime.UtcNow,
                            };

                            chat.ChatMessages.Add(chatMessage);
                            existingMessageIds.Add(syncMessage.MessageId);
                            newMessagesCount++;
                            anyChangeMade = true;
                        }
                    }
                }

                if(anyChangeMade || newMessagesCount > 0)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                return BaseResponse<SyncInitialMessagesModelResponse>.Success(
                    $"Successfully synced {newMessagesCount} new message(s)",
                    new SyncInitialMessagesModelResponse()
                );
            }
            catch (Exception ex)
            {
                return BaseResponse<SyncInitialMessagesModelResponse>.Error("An error occurred.");
            }
        }
    }
}
