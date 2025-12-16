using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.AccountServer;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service;
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
    internal class LocalServerHearbeatModelRequestHandler(ApplicationDbContext dbContext, IMediator _mediator, CurrentUserService _currentUserService, IHubContext<ChatHub> _hubContext) : IRequestHandler<LocalServerHeartbeatModelRequest, BaseResponse<LocalServerHeartbeatModelResponse>>
    {
        public async Task<BaseResponse<LocalServerHeartbeatModelResponse>> Handle(LocalServerHeartbeatModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check
            if (currentUser is null)
            {
                return BaseResponse<LocalServerHeartbeatModelResponse>.Error("Invalid request, please login again to continue");
            }

            var localServer = await dbContext.LocalServers
                                            .Include(a => a.Accounts)
                                            .Include(u => u.User)
                                            .ThenInclude(s => s.Subscriptions)
                                            .Include(u => u.User)
                                            .ThenInclude(ls => ls.LocalServers)
                                            .FirstOrDefaultAsync(ls => ls.UniqueId == request.ServerId, cancellationToken);

            if (localServer is null)
            {
                return BaseResponse<LocalServerHeartbeatModelResponse>.Error("Local Server Not Found");
            }

            var accountsRegisteredToServer = localServer.Accounts;

            if (accountsRegisteredToServer is null || accountsRegisteredToServer.Count == 0)
            {
                return BaseResponse<LocalServerHeartbeatModelResponse>.Error("No accounts running on this server");
            }

            var signalRNotification = new List<AccountStatusSignalRModel>();

            var activeAccountIdsFromHeartbeat = request.ActiveAccountIds;

            //Case 1: Everthing is on the right track.
            var accountsStillRunning = accountsRegisteredToServer.Where(a => activeAccountIdsFromHeartbeat.Contains(a.Id)).ToList();


            //Case 2: local server is telling i have 5 accounts but in DB we have 10 accounts mark them inactive and make them available and then try to run them. 
            var accountsNoLongerActive = accountsRegisteredToServer.Where(a => !activeAccountIdsFromHeartbeat.Contains(a.Id)).ToList();

            foreach (var notActiveAccount in accountsNoLongerActive)
            {
                notActiveAccount.ConnectionStatus = AccountConnectionStatus.Offline;
                notActiveAccount.LocalServerId = null;
                localServer.ActiveBrowserCount--;

                //preparing model to inform our app about the accounts via signalr
                var signalRModel = new AccountStatusSignalRModel()
                {
                    AccountId = notActiveAccount.Id,
                    ConnectionStatus= AccountConnectionStatus.Offline.GetInfo().Name
                };

                signalRNotification.Add(signalRModel);
            }

            //Case 3: local server is telling i have 10 accounts but in DB we have 5 accounts so update the status.
            var unreportedActiveAccountIds = activeAccountIdsFromHeartbeat.Where(a => !accountsRegisteredToServer.Any(lsa => lsa.Id == a)).ToList();

            var accountsToActivate = await dbContext.Accounts
                                                     .Where(a => unreportedActiveAccountIds.Contains(a.Id))
                                                     .ToListAsync(cancellationToken);

            foreach (var accountToActivate in accountsToActivate)
            {
                accountToActivate.ConnectionStatus = AccountConnectionStatus.Online;
                accountToActivate.LocalServerId = localServer.Id;
                localServer.ActiveBrowserCount++;

                //preparing model to inform our app about the accounts via signalr
                var signalRModel = new AccountStatusSignalRModel()
                {
                    AccountId = accountToActivate.Id,
                    ConnectionStatus= AccountConnectionStatus.Online.GetInfo().Name
                };

                signalRNotification.Add(signalRModel);
            }

            if (accountsNoLongerActive.Count > 0 || accountsToActivate.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            //Inform app about the accounts status
            if (signalRNotification.Count > 0)
            {
                await _hubContext.Clients.Group($"App_{currentUser.Id}")
                            .SendAsync("HandleAccountStatus", signalRNotification, cancellationToken);
            }

            //Try to launch accounts that were not active
            if (accountsNoLongerActive.Count > 0)
            {
                await _mediator.Send(new LaunchAccountsOnValidServerModelRequest() { AccountsToLaunch = accountsNoLongerActive }, cancellationToken);
            }

            return BaseResponse<LocalServerHeartbeatModelResponse>.Success("Operation performed successfully.", new LocalServerHeartbeatModelResponse());
        }
    }
}
