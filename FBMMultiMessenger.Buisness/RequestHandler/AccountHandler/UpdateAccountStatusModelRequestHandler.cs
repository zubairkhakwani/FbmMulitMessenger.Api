using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.Account;
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
        private readonly IHubContext<ChatHub> _hubContext;

        public UpdateAccountStatusModelRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext)
        {
            this._dbContext=dbContext;
            this._hubContext=hubContext;
        }

        public async Task<BaseResponse<UpdateAccountStatusModelResponse>> Handle(UpdateAccountStatusModelRequest request, CancellationToken cancellationToken)
        {
            if (request.Accounts is null || !request.Accounts.Any())
            {
                return BaseResponse<UpdateAccountStatusModelResponse>.Error("No account operations provided");
            }

            var accountIds = request.Accounts.Select(o => o.AccountId).ToList();

            var accounts = await _dbContext.Accounts
                                           .Include(u => u.User)
                                           .Where(a => accountIds.Contains(a.Id)).ToListAsync(cancellationToken);

            if (accounts.Count == 0)
            {
                return BaseResponse<UpdateAccountStatusModelResponse>.Error("Accounts not found");
            }

            var accountLookup = accounts.ToDictionary(a => a.Id);

            var userAccountSignals = new List<UserAccountSignalRModel>();

            foreach (var operation in request.Accounts)
            {
                if (!accountLookup.TryGetValue(operation.AccountId, out var account))
                    continue;

                account.Status = operation.Status;

                if (operation.FreeServer)
                {
                    account.LocalServerId = null;
                }

                var userSignal = userAccountSignals.FirstOrDefault(u => u.UserId == account.UserId);

                if (userSignal == null)
                {
                    userSignal = new UserAccountSignalRModel
                    {
                        UserId = account.UserId
                    };

                    userAccountSignals.Add(userSignal);
                }

                userSignal.AccountsStatus.Add(new AccountStatusSignalRModel
                {
                    AccountId = account.Id,
                    AccountStatus = AccountStatusExtension.GetInfo(operation.Status).Name
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            //Inform the user's app to update the status accordingly
            foreach (var userAccount in userAccountSignals)
            {
                var appId = $"App_{userAccount.UserId}";

                await _hubContext.Clients.Group(appId)
                    .SendAsync("HandleAccountStatus", userAccount.AccountsStatus, cancellationToken);
            }

            return BaseResponse<UpdateAccountStatusModelResponse>.Success("Account status updated successfully", new UpdateAccountStatusModelResponse());
        }
    }
}
