using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Extensions;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class UpdateAccountStatusModelRequestHandler : IRequestHandler<UpdateAccountStatusModelRequest, BaseResponse<UpdateAccountStatusModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISignalRService _signalRService;
        private readonly OneSignalService _oneSignalService;
        private readonly IEmailService _emailService;
        private readonly CurrentUserService _currentUserService;

        public UpdateAccountStatusModelRequestHandler(ApplicationDbContext dbContext, ISignalRService signalRService, OneSignalService oneSignalService, IEmailService emailService, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._signalRService=signalRService;
            this._oneSignalService=oneSignalService;
            this._emailService=emailService;
            this._currentUserService=currentUserService;
        }

        public async Task<BaseResponse<UpdateAccountStatusModelResponse>> Handle(UpdateAccountStatusModelRequest request, CancellationToken cancellationToken)
        {
            //Extra safety check: if the user has came to this point, it means he is authenticated, but we need to make sure of that before doing any operation
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser == null)
            {
                return BaseResponse<UpdateAccountStatusModelResponse>.Error("Invalid request, user is not authenticated");
            }
            var currentUserId = currentUser.Id;
            var incomingProxyId = request.ProxyId;

            if (incomingProxyId is null && request.Accounts.Count==0)
            {
                return BaseResponse<UpdateAccountStatusModelResponse>.Error("No account operations provided");
            }

            var accountIds = request.Accounts.Select(o => o.AccountId).ToList();

            var accounts = await _dbContext.Accounts
                                           .Include(ls => ls.LocalServer)
                                           .Include(u => u.User)
                                           .Where(a => accountIds.Contains(a.Id) && a.UserId == currentUserId).ToListAsync(cancellationToken);

            var user = accounts.FirstOrDefault()?.User;

            if (incomingProxyId is null && accounts.Count == 0)
            {
                return BaseResponse<UpdateAccountStatusModelResponse>.Error("Accounts not found");
            }

            var accountLookup = accounts.ToDictionary(a => a.Id);

            var userAccountSignals = new List<UserAccountSignalRModel>();

            var isDatabaseUpdated = false;

            //build signalR model and update the accounts according to the received operations
            foreach (var operation in request.Accounts)
            {
                if (!accountLookup.TryGetValue(operation.AccountId, out var account))
                {
                    continue;
                }

                isDatabaseUpdated = true;

                var connectionStatus = operation.ConnectionStatus;
                var authStatus = operation.AuthStatus;
                var logoutReason = operation.Reason;

                account.ConnectionStatus = connectionStatus;
                account.AuthStatus = authStatus;
                account.Reason = logoutReason;

                if (operation.FreeServer)
                {
                    account.LocalServerId = null;

                    var accountLocalServer = account.LocalServer;
                    if (accountLocalServer is not null)
                    {
                        accountLocalServer.ActiveBrowserCount--;
                    }
                }

                var userSignal = userAccountSignals.FirstOrDefault(u => u.AppId == account.UserId);

                if (userSignal == null)
                {
                    userSignal = new UserAccountSignalRModel
                    {
                        AppId = account.UserId
                    };

                    userAccountSignals.Add(userSignal);
                }

                userSignal.AccountsStatus.Add(new AccountStatusSignalRModel
                {
                    AccountId = account.Id,
                    AccountName = account.Name,

                    Reason = logoutReason,
                    ReasonText = logoutReason.GetInfo().Name,

                    ConnectionStatus = connectionStatus,
                    ConnectionStatusText =  connectionStatus.GetInfo().Name,

                    AuthStatus = authStatus,
                    AuthStatusText = authStatus.GetInfo().Name,

                    IsConnected = operation.AuthStatus == AccountAuthStatus.LoggedIn
                });
            }

            if (isDatabaseUpdated)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            //Inform app about the accounts status
            await _signalRService.NotifyAppAccountStatus(userAccountSignals, cancellationToken);

            //Send push notifications if the account is logged out
            await SendAccountLogoutPushNotification(userAccountSignals);

            //Send Email if there are logged out accounts
            _= SendAccountLogoutEmail(userAccountSignals, user);

            // If the request contains proxyId, it means that the proxy related to this id is misconfigured, so we need to update the accounts related to this proxy and inform the user about that
            await HandleProxyMisconfiguration(request.ProxyId, currentUserId, cancellationToken);

            return BaseResponse<UpdateAccountStatusModelResponse>.Success("Account status updated successfully", new UpdateAccountStatusModelResponse());
        }

        private async Task SendAccountLogoutEmail(List<UserAccountSignalRModel> userAccountSignals, User? user)
        {
            var userAccounts = userAccountSignals.SelectMany(a => a.AccountsStatus).ToList();
            var loggedOutAccounts = new List<AccountStatusSignalRModel>();

            foreach (var userAccountSignal in userAccounts)
            {
                if (userAccountSignal.AuthStatus == AccountAuthStatus.LoggedOut)
                {
                    loggedOutAccounts.Add(userAccountSignal);
                }
            }
            if (user is not null && loggedOutAccounts.Count > 0)
            {
                _=_emailService.SendAccountLogoutEmailAsync(user.Email, user.Name, loggedOutAccounts);
            }
        }

        private async Task SendAccountLogoutPushNotification(List<UserAccountSignalRModel> userAccountSignals)
        {
            foreach (var userAccountSignal in userAccountSignals)
            {
                var userId = userAccountSignal.AppId;
                var accountStatuses = userAccountSignal.AccountsStatus;

                foreach (var accountStatus in accountStatuses)
                {
                    if (accountStatus.AuthStatus == AccountAuthStatus.LoggedOut)
                    {
                        var message = $"{accountStatus.AccountName} has been logged out. Check your email for more details";

                        _ =  _oneSignalService.PushLogoutNotificationAsync(userId.ToString(), message, accountStatus.AccountId.ToString());
                    }
                }
            }
        }

        private async Task HandleProxyMisconfiguration(int? proxyId, int currentUserId, CancellationToken cancellationToken)
        {
            if (proxyId is null)
            {
                return;
            }

            var proxy = await _dbContext
                                .Proxies
                                .Include(a => a.Accounts)
                                .FirstOrDefaultAsync(p => p.Id == proxyId
                                                    &&
                                                    p.UserId == currentUserId, cancellationToken);

            if (proxy is null)
            {
                return;
            }
            var proxyName = proxy.Name;
            var accountsCount = proxy.Accounts.Count();
            var accountText = accountsCount == 1 ? "account" : "accounts";

            var message = $"The proxy '{proxyName}' appears to be invalid or misconfigured. " +
               $"It is currently assigned to {accountsCount} {accountText}. " +
               $"Please review and update the proxy configuration.";

            var userAccountSignals = new List<UserAccountSignalRModel>();

            var isDatabaseUpdated = false;

            foreach (var account in proxy.Accounts)
            {
                account.AuthStatus = AccountAuthStatus.Idle;
                account.Reason = AccountReason.InvalidProxy;

                isDatabaseUpdated = true;

                var userSignal = userAccountSignals.FirstOrDefault(u => u.AppId == account.UserId);

                if (userSignal == null)
                {
                    userSignal = new UserAccountSignalRModel
                    {
                        AppId = account.UserId
                    };

                    userAccountSignals.Add(userSignal);
                }

                userSignal.AccountsStatus.Add(new AccountStatusSignalRModel
                {
                    AccountId = account.Id,
                    AccountName = account.Name,

                    Reason = AccountReason.InvalidProxy,
                    ReasonText = AccountReason.InvalidProxy.GetInfo().Name,

                    AuthStatus = AccountAuthStatus.Idle,
                    AuthStatusText = AccountAuthStatus.Idle.GetInfo().Name,

                    IsConnected = true
                });
            }

            if (isDatabaseUpdated)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            //Inform app about the accounts status
            await _signalRService.NotifyAppAccountStatus(userAccountSignals, cancellationToken);


            await _oneSignalService.ProxyNotWorkingNotificationAsync(currentUserId, message);
        }
    }
}
