using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class GetAccountsStatusModelRequestHandler : IRequestHandler<GetAccountsStatusModelRequest, BaseResponse<GetAccountsStatusModelResponse>>
    {
        private readonly ApplicationDbContext dbContext;
        private readonly CurrentUserService currentUserService;

        public GetAccountsStatusModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this.dbContext = dbContext;
            this.currentUserService = currentUserService;
        }

        public async Task<BaseResponse<GetAccountsStatusModelResponse>> Handle(GetAccountsStatusModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = currentUserService.GetCurrentUser();

            var statuses = await dbContext.Accounts.Where(a => a.UserId == currentUser.Id)
                .Select(a => new AccountStatusResponse
                {
                    Id = a.Id,
                    IsConnected = (a.AuthStatus == AccountAuthStatus.LoggedIn)
                })
                .ToListAsync();

            var response = new GetAccountsStatusModelResponse()
            {
                Statuses = statuses
            };

            return BaseResponse<GetAccountsStatusModelResponse>.Success("", response);
        }
    }
}
