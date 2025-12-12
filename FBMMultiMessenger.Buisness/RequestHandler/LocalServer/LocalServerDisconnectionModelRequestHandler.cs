using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Extensions;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.LocalServer
{
    //TOODO
    internal class LocalServerDisconnectionModelRequestHandler : IRequestHandler<LocalServerDisconnectionModelRequest, BaseResponse<LocalServerDisconnectionModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;

        public LocalServerDisconnectionModelRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext)
        {
            this._dbContext=dbContext;
            this._hubContext=hubContext;
        }
        public async Task<BaseResponse<LocalServerDisconnectionModelResponse>> Handle(LocalServerDisconnectionModelRequest request, CancellationToken cancellationToken)
        {
            var localServer = await _dbContext.LocalServers
                                              .Include(a => a.Accounts)
                                              .Include(u => u.User)
                                              .ThenInclude(s => s.LocalServers)
                                              .FirstOrDefaultAsync(ls => ls.UniqueId == request.UniqueId, cancellationToken);

            if (localServer is null)
            {
                return BaseResponse<LocalServerDisconnectionModelResponse>.Error("Local server not found.");
            }

            //Mark this server as offline
            localServer.IsOnline = false;

            var accounts = localServer.Accounts;

            //Get other local servers of the user
            var userLocalServers = localServer.User.LocalServers.Where(x => x.Id != localServer.Id).ToList();

            var powerfulServer = LocalServerHelper.GetAvailablePowerfulServer(userLocalServers);

            var userId = localServer.UserId;

            var accountStatusModel = new AccountsStatusSignalRModel();

            if (powerfulServer is null)
            {
                var reason = userLocalServers is null || userLocalServers.Count == 0
                    ? "All accounts set to inactive as there are no other local servers available."
                    : "No available server found. All servers are either inactive or at full capacity.";

                foreach (var account in accounts)
                {
                    account.LocalServerId = null;
                    account.Status = AccountStatus.Inactive;

                    accountStatusModel.AccountStatus.Add(account.Id, AccountStatusExtension.GetInfo(AccountStatus.Inactive).Name);
                }

                await _hubContext.Clients.Group($"App_{userId}")
                    .SendAsync("HandleAccountStatus", accountStatusModel, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                return BaseResponse<LocalServerDisconnectionModelResponse>.Success(reason, new LocalServerDisconnectionModelResponse());
            }

            var remainingSlots = powerfulServer.MaxBrowserCapacity - powerfulServer.ActiveBrowserCount;

            var accountsToMove = accounts.Take(remainingSlots).ToList();

            var remainingAccounts = accounts.Skip(remainingSlots).ToList();

            foreach (var account in accountsToMove)
            {
                account.LocalServerId = powerfulServer.Id;
                account.Status = AccountStatus.InProgress;

                //Prepare SignalR model
                accountStatusModel.AccountStatus.Add(account.Id, AccountStatusExtension.GetInfo(AccountStatus.InProgress).Name);
            }

            foreach (var account in remainingAccounts)
            {
                account.LocalServerId = null;
                account.Status = AccountStatus.Inactive;

                //Prepare SignalR model
                accountStatusModel.AccountStatus.Add(account.Id, AccountStatusExtension.GetInfo(AccountStatus.Inactive).Name);
            }


            await _dbContext.SaveChangesAsync(cancellationToken);

            await _hubContext.Clients.Group($"App_{userId}")
                .SendAsync("HandleAccountStatus", accountStatusModel, cancellationToken);

            return BaseResponse<LocalServerDisconnectionModelResponse>.Success("Accounts have been reassigned to another server or set to inactive based on server capacity.", new LocalServerDisconnectionModelResponse());
        }
    }
}
