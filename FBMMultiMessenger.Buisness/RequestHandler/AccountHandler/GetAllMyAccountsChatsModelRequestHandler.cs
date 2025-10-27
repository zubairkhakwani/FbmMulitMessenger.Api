using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Response;
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
                                        .Include(cm => cm.ChatMessages)
                                        .Where(u => u.UserId == currentUser.Id)
                                        .OrderByDescending(x => x.UpdatedAt)
                                        .ToListAsync(cancellationToken);

            var formattedChats = chats.Select(x =>
            {
                var lastMessageInfo = GetLastMessageInfo(x.FbListingTitle, x.ChatMessages);

                return new GetMyChatsModelResponse
                {
                    Id = x.Id,
                    FbChatId = x.FBChatId!,
                    FbListingTitle = x.FbListingTitle,
                    FbListingLocation = x.FbListingLocation,
                    FbListingPrice = x.FbListingPrice,
                    FbListingImage = x.FBListingImage,
                    UserProfileImage = x.UserProfileImage,
                    LastMessage = lastMessageInfo.lastMessage,
                    LastMessageFrom = lastMessageInfo.lastMessageFrom,
                    IsRead = x.IsRead,
                    UnReadCount = x.ChatMessages.Count(m => !m.IsRead),
                    StartedAt = x.StartedAt
                };
            }).ToList();



            var response = new GetAllMyAccountsChatsModelResponse()
            {
                Chats = formattedChats
            };

            return BaseResponse<GetAllMyAccountsChatsModelResponse>.Success("Operation performed successfully", response);
        }
        public (string? lastMessage, string? lastMessageFrom) GetLastMessageInfo(string? title, List<ChatMessages> chatMessages)
        {
            var lastMessage = chatMessages.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

            if (lastMessage is not null && title is not null)
            {
                string lastMessageFrom = lastMessage.IsReceived ? title.Split(" ")[0] : "You";

                return (lastMessage.Message, lastMessageFrom);
            }

            return (null, null);
        }
    }
}
