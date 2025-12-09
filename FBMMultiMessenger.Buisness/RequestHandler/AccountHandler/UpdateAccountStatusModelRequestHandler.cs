using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Extensions;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class UpdateAccountStatusModelRequestHandler : IRequestHandler<UpdateAccountStatusModelRequest, BaseResponse<UpdateAccountStatusModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;

        public UpdateAccountStatusModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> hubContext)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
            this._hubContext=hubContext;
        }

        public async Task<BaseResponse<UpdateAccountStatusModelResponse>> Handle(UpdateAccountStatusModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check
            if (currentUser is null)
            {
                return BaseResponse<UpdateAccountStatusModelResponse>.Error("Invalid Request, Please login again to continue");
            }

            var accountIds = request.AccountStatus?.Keys.ToList() ?? new List<int>();

            if (request.AccountStatus is null || !request.AccountStatus.Any())
            {
                return BaseResponse<UpdateAccountStatusModelResponse>.Error("No accounts provided to update");
            }

            var accounts = await _dbContext.Accounts
                                           .Where(a => accountIds.Contains(a.Id)).ToListAsync(cancellationToken);

            if (accounts is null)
            {
                return BaseResponse<UpdateAccountStatusModelResponse>.Error("Account not found");
            }

            var accountsStatusSignal = new AccountsStatusSignalRModel();

            foreach (var account in accounts)
            {
                if (request.AccountStatus.TryGetValue(account.Id, out var newStatus))
                {
                    account.Status = newStatus;

                    accountsStatusSignal.AccountStatus.Add(account.Id, AccountStatusExtension.GetInfo(newStatus).Name);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _hubContext.Clients.Group($"App_{currentUser.Id}")
                             .SendAsync("HandleAccountStatus", accountsStatusSignal, cancellationToken);

            return BaseResponse<UpdateAccountStatusModelResponse>.Success("Account status updated successfully", new UpdateAccountStatusModelResponse());
        }
    }
}
