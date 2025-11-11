using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Request.DefaultMessage;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Contracts.DefaultMessage;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FBMMultiMessenger.Buisness.RequestHandler.DefaultMessage
{
    internal class GetMyDefaultMessagesModelRequestHandler : IRequestHandler<GetMyDefaultMessagesModelRequest, BaseResponse<GetMyDefaultMessagesModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetMyDefaultMessagesModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<GetMyDefaultMessagesModelResponse>> Handle(GetMyDefaultMessagesModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check, if the use has came to this point he will be logged in hence current user can never be null.
            if (currentUser is null)
            {
                return BaseResponse<GetMyDefaultMessagesModelResponse>.Error("Invalid Request, please login again to continue.");
            }

            var currentUserId = currentUser.Id;


            var user = await _dbContext.Users
                                  .Include(a => a.Accounts)
                                  .Include(x => x.DefaultMessages)
                                  .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);


            var allAccounts = user?.Accounts.Select(x => new GetMyAccountsModelResponse()
            {
                Id = x.Id,
                Name = x.Name,
                Cookie = x.Cookie,
                CreatedAt = x.CreatedAt
            }).ToList() ?? new List<GetMyAccountsModelResponse>();

            var defaultMessages = user?.DefaultMessages.Select(x => new DefaultMessagesModelResponse()
            {
                Id = x.Id,
                Message = x.Message,
                CreatedAt = x.CreatedAt,
                Accounts = x.Accounts.Select(x => new GetMyAccountsModelResponse()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Cookie = x.Cookie,
                    CreatedAt = x.CreatedAt
                }).ToList()
            }).ToList() ?? new List<DefaultMessagesModelResponse>();


            var response = new GetMyDefaultMessagesModelResponse()
            {
                DefaultMessages = defaultMessages,
                AllAccounts = allAccounts,
            };
            return BaseResponse<GetMyDefaultMessagesModelResponse>.Success("Operation performed successfully.", response);

        }
    }
}
