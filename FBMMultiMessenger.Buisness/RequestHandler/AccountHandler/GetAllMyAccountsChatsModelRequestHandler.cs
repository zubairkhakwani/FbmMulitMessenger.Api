using FBMMultiMessenger.Buisness.Exntesions;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class GetAllMyAccountsChatsModelRequestHandler : IRequestHandler<GetAllMyAccountsChatsModelRequest, BaseResponse<GetAllMyAccountsChatsModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetAllMyAccountsChatsModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext = dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<GetAllMyAccountsChatsModelResponse>> Handle(GetAllMyAccountsChatsModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currenuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<GetAllMyAccountsChatsModelResponse>.Error("Invalid Request, Please login again to continue.");
            }

            var chats = await _dbContext.Chats
                                        .Include(a => a.Account)
                                        .Include(cm => cm.ChatMessages)
                                        .AsNoTracking()
                                        .Where(u => u.UserId == currentUser.Id)
                                        .OrderByDescending(x => x.ChatMessages.Max(cm => (long?)cm.FBTimestamp))
                                        .ToListAsync(cancellationToken);


            var formattedChats = chats.Select(x =>
            {
                var lastMessage = x.ChatMessages
                                   .OrderByDescending(x => x.FBTimestamp)
                                   .FirstOrDefault()
                                    ??
                                    new ChatMessages() { Message= string.Empty };

                var account = x.Account;
                var isAccountConnected = account is not null && account.AuthStatus == AccountAuthStatus.LoggedIn;
                var chatAccount = account is null ? null : new GetMyChatAccountModelResponse()
                {
                    Id = account.Id,
                    Name = account.Name,
                    CreatedAt = account.CreatedAt
                };

                var messagePreview = ChatMessagesHelper.GetMessagePreview(lastMessage.ToMessagePreviewRequest(x.OtherUserName ?? ""));

                return new GetMyChatsModelResponse
                {
                    ChatId = x.Id,
                    FbChatId = x.FBChatId!,
                    FbListingTitle = x.FbListingTitle,
                    FbListingLocation = x.FbListingLocation,
                    FbListingPrice = x.FbListingPrice,
                    FbListingImage = x.FBListingImage,
                    UserProfileImage = x.UserProfileImage,
                    MessagePreview = messagePreview.MessagPreview,
                    SenderName = messagePreview.SenderName,
                    ChattingWithName = x.OtherUserName,
                    ChattingWithId = x.OtherUserId,
                    IsRead = x.IsRead,
                    UnReadCount = x.ChatMessages.Count(m => !m.IsRead),
                    StartedAt = x.StartedAt,
                    IsAccountConnected = isAccountConnected,
                    Account = chatAccount
                };

            }).ToList();

            var response = new GetAllMyAccountsChatsModelResponse()
            {
                Chats = formattedChats
            };

            return BaseResponse<GetAllMyAccountsChatsModelResponse>.Success("Operation performed successfully", response);
        }
    }
}
