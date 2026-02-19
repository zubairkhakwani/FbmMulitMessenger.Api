using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Extensions;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class ConnectAccountModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, ISubscriptionServerProviderService _subscriptionServerProviderService, ILocalServerService _localServerService, IUserAccountService _accountService, ISignalRService _signalRService) : IRequestHandler<ConnectAccountModelRequest, BaseResponse<object>>
    {
        public async Task<BaseResponse<object>> Handle(ConnectAccountModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            var currentUserId = currentUser!.Id;

            var account = await _dbContext.Accounts
                                          .Include(dm => dm.DefaultMessage)
                                          .Include(u => u.User)
                                          .ThenInclude(s => s.Subscriptions)
                                          .FirstOrDefaultAsync(x => x.Id == request.AccountId
                                                         &&
                                                         x.UserId == currentUserId, cancellationToken);

            if (account is null)
            {
                return BaseResponse<object>.Error("Invalid request, Account does not exist.");
            }

            var accountLocalServer = account.LocalServer;

            if ((accountLocalServer is not null && account.AuthStatus == AccountAuthStatus.LoggedIn))
            {
                return BaseResponse<object>.Error("Account is already connected and running");
            }

            var userSubscriptions = account.User.Subscriptions;

            var activeSubscription = _accountService.GetActiveSubscription(userSubscriptions)
                                     ??
                                     _accountService.GetLastActiveSubscription(userSubscriptions);

            var eligibleServers = await _subscriptionServerProviderService.GetEligibleServersAsync(activeSubscription!);

            var powerfullEligibleServers = _localServerService.GetPowerfulServers(eligibleServers);

            var assignedServer = _localServerService.GetLeastLoadedServer(powerfullEligibleServers);

            if (assignedServer is null)
            {
                return BaseResponse<object>.Error("Unable to launch account. Please ensure your local server is running and has available capacity.");
            }

            var newAccountHttpResponse = new LocalServerAccountDTO()
            {
                Id =  account.Id,
                Name = account.Name,
                Cookie = account.Cookie,
                DefaultMessage = account.DefaultMessage?.Message,
                CreatedAt = account.CreatedAt
            };

            account.ConnectionStatus = AccountConnectionStatus.Starting;
            account.AuthStatus = AccountAuthStatus.Idle;
            account.Reason = AccountReason.AssigningToLocalServer;

            account.LocalServerId = assignedServer.Id;

            assignedServer.ActiveBrowserCount++;

            await _dbContext.SaveChangesAsync(cancellationToken);

            //Inform app to update account status
            var userAccountSignalR = new UserAccountSignalRModel()
            {
                AppId = account.UserId,
                AccountsStatus = new List<AccountStatusSignalRModel>()
                {
                    new AccountStatusSignalRModel()
                    {
                        AccountId = account.Id,
                        ConnectionStatusText = account.ConnectionStatus.GetInfo().Name,
                        AuthStatusText = account.AuthStatus.GetInfo().Name,
                        ReasonText = account.Reason.GetInfo().Name
                    }
                }
            };

            var appId = $"App_{account.UserId}";
            //Send account status update to app
            await _signalRService.NotifyAppAccountStatus([userAccountSignalR], cancellationToken);

            //Inform local server to open browser if not opened.
            await _signalRService.NotifyLocalServerAccountConnect(newAccountHttpResponse, assignedServer.UniqueId, cancellationToken);

            return BaseResponse<object>.Success("Please wait, account is being connected.", new object());
        }
    }
}
