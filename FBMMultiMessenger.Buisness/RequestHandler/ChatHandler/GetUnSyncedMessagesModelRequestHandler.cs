using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Request.Subscription;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class GetUnSyncedMessagesModelRequestHandler : IRequestHandler<GetUnSyncedMessagesModelRequest, BaseResponse<GetUnSyncedMessagesModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IMediator mediator;

        public GetUnSyncedMessagesModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IMediator mediator)
        {
            this._dbContext = dbContext;
            this._currentUserService = currentUserService;
            this.mediator = mediator;
        }

        public async Task<BaseResponse<GetUnSyncedMessagesModelResponse>> Handle(GetUnSyncedMessagesModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currenuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<GetUnSyncedMessagesModelResponse>.Error("Invalid Request, Please login again to continue.");
            }

            var lastSyncedAt = DateTimeOffset.UtcNow;

            var subscriptionResult = await mediator.Send(new GetMySubscriptionModelRequest());
            if (!subscriptionResult.IsSuccess || !subscriptionResult.Data.HasActiveSubscription)
            {
                var response = new GetUnSyncedMessagesModelResponse()
                {
                    HasActiveSubscription = false,
                    Accounts = new(),
                    Chats = new(),
                };

                return BaseResponse<GetUnSyncedMessagesModelResponse>.Error("You can only view your old messages, but new messages will only be shown when you have an active subscription", redirectToPackages: false, showSweetAlert: false, result: response);
            }

            try
            {
                var chats = await _dbContext.Chats
                                        .Include(cm => cm.ChatMessages)
                                        .AsNoTracking()
                                        .Where(c => c.UserId == currentUser.Id && (request.LastSyncedMessageAt == null || c.UpdatedAt >= request.LastSyncedMessageAt))
                                        .OrderByDescending(x => x.UpdatedAt)
                                        .ToListAsync(cancellationToken);

                var accounts = await _dbContext.Accounts
                                        .AsNoTracking()
                                        .Where(c => c.UserId == currentUser.Id && (request.LastSyncedMessageAt == null || c.CreatedAt >= request.LastSyncedMessageAt || c.UpdatedAt >= request.LastSyncedMessageAt))
                                        .ToListAsync(cancellationToken);

                var responseAccounts = accounts.Select(a => new SyncAccount
                {
                    Id = a.Id,
                    Name = a.Name,
                    FbAccountId = a.FbAccountId,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                }).ToList();

                foreach (var chat in chats)
                {
                    chat.ChatMessages = chat.ChatMessages.Where(cm => (request.LastSyncedMessageAt == null || cm.CreatedAt >= request.LastSyncedMessageAt || cm.UpdatedAt >= request.LastSyncedMessageAt)).ToList();
                }

                var responseChats = chats.Select(c => new SyncChat()
                {
                    Id = c.Id,
                    AccountId = c.AccountId,
                    UserId = c.UserId,
                    FbUserId = c.FbUserId,
                    FbAccountId = c.FbAccountId,
                    FBChatId = c.FBChatId,
                    FbListingId = c.FbListingId,
                    FbListingTitle = c.FbListingTitle,
                    FbListingLocation = c.FbListingLocation,
                    FBListingImage = c.FBListingImage,
                    UserProfileImage = c.UserProfileImage,
                    OtherUserName = c.OtherUserName,
                    OtherUserId = c.OtherUserId,
                    FbListingPrice = c.FbListingPrice,
                    IsRead = c.IsRead,
                    StartedAt = c.StartedAt,
                    UpdatedAt = c.UpdatedAt,
                    ChatMessages = c.ChatMessages.Select(cm => new SyncChatMessages()
                    {
                        Id = cm.Id,
                        ChatId = cm.ChatId,
                        FbMessageId = cm.FbMessageId,
                        FbMessageReplyId = cm.FbMessageReplyId,
                        FBTimestamp = cm.FBTimestamp,
                        Message = cm.Message,
                        IsReceived = cm.IsReceived,
                        IsRead = cm.IsRead,
                        IsSent = cm.IsSent,
                        IsTextMessage = cm.IsTextMessage,
                        IsImageMessage = cm.IsImageMessage,
                        IsVideoMessage = cm.IsVideoMessage,
                        IsAudioMessage = cm.IsAudioMessage,
                        CreatedAt = cm.CreatedAt,
                        UpdatedAt = cm.UpdatedAt,
                    }).ToList()

                }).ToList();

                var response = new GetUnSyncedMessagesModelResponse()
                {
                    HasActiveSubscription = true,
                    Accounts = responseAccounts,
                    Chats = responseChats,
                    LastSyncedAt = lastSyncedAt
                };

                return BaseResponse<GetUnSyncedMessagesModelResponse>.Success("", response, redirectToPackages: false, showSweetAlert: false);
            }
            catch(Exception ex)
            {
                return BaseResponse<GetUnSyncedMessagesModelResponse>.Error("An error occured while processing your request. please contact administrator.");
            }
        }
    }
}
