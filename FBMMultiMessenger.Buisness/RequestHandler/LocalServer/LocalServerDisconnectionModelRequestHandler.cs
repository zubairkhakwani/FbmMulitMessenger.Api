using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service.IServices;
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
    internal class LocalServerDisconnectionModelRequestHandler(ApplicationDbContext _dbContext, IUserAccountService _userAccountService, ILocalServerService _localServerService, ISubscriptionServerProviderService _subscriptionServerProviderService, IHubContext<ChatHub> _hubContext) : IRequestHandler<LocalServerDisconnectionModelRequest, BaseResponse<LocalServerDisconnectionModelResponse>>
    {
        public async Task<BaseResponse<LocalServerDisconnectionModelResponse>> Handle(LocalServerDisconnectionModelRequest request, CancellationToken cancellationToken)
        {
            var localServer = await _dbContext.LocalServers
                                              .Include(a => a.Accounts)
                                              .Include(u => u.User)
                                              .ThenInclude(s => s.Subscriptions)
                                              .FirstOrDefaultAsync(ls => ls.UniqueId == request.UniqueId, cancellationToken);

            if (localServer is null)
            {
                return BaseResponse<LocalServerDisconnectionModelResponse>.Error("Local server not found.");
            }

            //Mark this server as offline
            localServer.IsOnline = false;

            var userSubscriptions = localServer.User.Subscriptions;

            var activeSubscription = _userAccountService.GetActiveSubscription(userSubscriptions)
                                      ??
                                     _userAccountService.GetLastActiveSubscription(userSubscriptions);


            // Defensive check — activeSubscription should never be null here (as for current buisness rule),
            if (activeSubscription is null)
            {
                return BaseResponse<LocalServerDisconnectionModelResponse>.Error("User does not have any active subscription.");
            }

            //Get server based on user subscription
            var eligibleServers = await _subscriptionServerProviderService.GetEligibleServersAsync(activeSubscription);

            var powerfullEligibleServers = _localServerService.GetPowerfulServers(eligibleServers);

            var userId = localServer.UserId;

            var accountStatusModel = new List<AccountStatusSignalRModel>();

            //Accounts that were running on this server
            var localServerAccounts = localServer.Accounts;

            //If there is no server that can launch the accounts that are being disconnected.
            if (powerfullEligibleServers is null || powerfullEligibleServers.Count == 0)
            {
                foreach (var account in localServerAccounts)
                {
                    account.LocalServerId = null;
                    account.ConnectionStatus = AccountConnectionStatus.Offline;
                    account.AuthStatus = AccountAuthStatus.Idle;
                    account.Reason = AccountReason.NotAssignedToAnyLocalServer;

                    //Sync active browser count that were running on this server.
                    localServer.ActiveBrowserCount--;

                    accountStatusModel.Add(
                        new AccountStatusSignalRModel()
                        {
                            AccountId = account.Id,
                            ConnectionStatusText = AccountConnectionStatus.Offline.GetInfo().Name,
                            AuthStatusText = AccountAuthStatus.Idle.GetInfo().Name,
                            ReasonText = AccountReason.NotAssignedToAnyLocalServer.GetInfo().Name,
                            IsConnected = false
                        });
                }

                await _hubContext.Clients.Group($"App_{userId}")
                    .SendAsync("HandleAccountStatus", accountStatusModel, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                return BaseResponse<LocalServerDisconnectionModelResponse>.Success("All accounts set to inactive as there are no other local servers available.", new LocalServerDisconnectionModelResponse());
            }

            //If we do have servers 
            foreach (var account in localServerAccounts)
            {
                var leastLoadedServer = _localServerService.GetLeastLoadedServer(powerfullEligibleServers);

                var canAssignServer =
                                    leastLoadedServer is not null &&
                                    leastLoadedServer.IsOnline &&
                                    leastLoadedServer.ActiveBrowserCount < leastLoadedServer.MaxBrowserCapacity;

                if (canAssignServer)
                {
                    account.LocalServerId = leastLoadedServer!.Id;
                    account.ConnectionStatus = AccountConnectionStatus.Starting;
                    account.AuthStatus  = AccountAuthStatus.Idle;
                    account.Reason = AccountReason.AssigningToLocalServer;

                    leastLoadedServer.ActiveBrowserCount++;

                    //Prepare SignalR model
                    accountStatusModel.Add(
                        new AccountStatusSignalRModel()
                        {
                            AccountId = account.Id,
                            ConnectionStatusText = AccountConnectionStatus.Starting.GetInfo().Name,
                            AuthStatusText = AccountAuthStatus.Idle.GetInfo().Name,
                            ReasonText  = AccountReason.AssigningToLocalServer.GetInfo().Name,
                            IsConnected = false
                        });
                }
                else
                {
                    account.LocalServerId = null;
                    account.ConnectionStatus = AccountConnectionStatus.Offline;
                    account.AuthStatus = AccountAuthStatus.Idle;
                    account.Reason = AccountReason.NotAssigned_ServerOffline;

                    //Sync active browser count that were running on this server.
                    localServer.ActiveBrowserCount--;

                    //Prepare SignalR model
                    accountStatusModel.Add(
                        new AccountStatusSignalRModel()
                        {
                            AccountId = account.Id,
                            ConnectionStatusText = AccountConnectionStatus.Offline.GetInfo().Name,
                            AuthStatusText = AccountAuthStatus.Idle.GetInfo().Name,
                            ReasonText = AccountReason.NotAssigned_ServerOffline.GetInfo().Name,
                            IsConnected = false
                        });
                }
            }


            await _dbContext.SaveChangesAsync(cancellationToken);

            await _hubContext.Clients.Group($"App_{userId}")
                .SendAsync("HandleAccountStatus", accountStatusModel, cancellationToken);

            return BaseResponse<LocalServerDisconnectionModelResponse>.Success("Accounts have been reassigned to another server or set to inactive based on server capacity.", new LocalServerDisconnectionModelResponse());
        }
    }
}
