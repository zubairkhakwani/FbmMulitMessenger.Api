using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Request.AccountServer;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.LocalServer
{
    internal class MonitorLocalServerHeartbeatModelRequestHandler : IRequestHandler<MonitorLocalServerHearbeatModelRequest, BaseResponse<MonitorLocalServerHearbeatModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMediator _mediator;

        public MonitorLocalServerHeartbeatModelRequestHandler(ApplicationDbContext dbContext, IMediator mediator)
        {
            this._dbContext=dbContext;
            this._mediator=mediator;
        }
        public async Task<BaseResponse<MonitorLocalServerHearbeatModelResponse>> Handle(MonitorLocalServerHearbeatModelRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var localServers = await _dbContext.LocalServers
                                                       .Include(ls => ls.Accounts)
                                                       .Include(ls => ls.User)
                                                       .ThenInclude(u => u.Subscriptions)
                                                       .Where(ls => ls.IsActive)
                                                       .ToListAsync(cancellationToken);

                //get servers that are online => servers gets disconnected if the API was down and servers were able to establish  SIGNALR connection.
                var onlineServers = localServers.Where(x => x.IsOnline).ToList();

                //get servers that are offline => servers gets disconnected and servers were not able to establish SIGNALR connection. 
                var offlineServers = localServers.Where(ls => !ls.IsOnline).ToList();

                var offlineServersAccounts = offlineServers.SelectMany(os => os.Accounts).ToList();

                var updateAccountStatus = offlineServersAccounts.Select(a => new AccountUpdateModelOperation()
                {
                    AccountId = a.Id,
                    ConnectionStatus = AccountConnectionStatus.Offline,
                    AuthStatus = AccountAuthStatus.Idle,
                    FreeServer = true
                }).ToList();

                //mark them inactive, make them available and inform the app to update the status.
                await _mediator.Send(new UpdateAccountStatusModelRequest() { Accounts = updateAccountStatus }, cancellationToken);

                //try to launch these accounts on available server based on the subscription => can launch on our super servers or user's own servers.
                await _mediator.Send(new LaunchAccountsOnValidServerModelRequest() { AccountsToLaunch = offlineServersAccounts }, cancellationToken);

                return BaseResponse<MonitorLocalServerHearbeatModelResponse>.Success("local server monitoring successfull", new MonitorLocalServerHearbeatModelResponse());
            }

            catch (Exception)
            {
                return BaseResponse<MonitorLocalServerHearbeatModelResponse>.Error("Unexpected error while monitoring local server");
            }
        }
    }
}
