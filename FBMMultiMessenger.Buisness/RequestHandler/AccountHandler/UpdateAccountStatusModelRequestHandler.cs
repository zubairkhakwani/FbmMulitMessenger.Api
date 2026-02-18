using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Enums;
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
        private readonly ISignalRService _signalRService;
        private readonly OneSignalService _oneSignalService;
        private readonly IUserAccountService _userAccountService;
        private readonly IEmailService _emailService;

        public UpdateAccountStatusModelRequestHandler(ApplicationDbContext dbContext, ISignalRService signalRService, OneSignalService oneSignalService, IUserAccountService userAccountService, IEmailService emailService)
        {
            this._dbContext=dbContext;
            this._signalRService=signalRService;
            this._oneSignalService=oneSignalService;
            this._userAccountService=userAccountService;
            this._emailService=emailService;
        }

        public async Task<BaseResponse<UpdateAccountStatusModelResponse>> Handle(UpdateAccountStatusModelRequest request, CancellationToken cancellationToken)
        {
            if (request.Accounts is null || !request.Accounts.Any())
            {
                return BaseResponse<UpdateAccountStatusModelResponse>.Error("No account operations provided");
            }

            var accountIds = request.Accounts.Select(o => o.AccountId).ToList();

            var accounts = await _dbContext.Accounts
                                           .Include(ls => ls.LocalServer)
                                           .Include(u => u.User)
                                           .ThenInclude(s => s.Subscriptions)
                                           .Where(a => accountIds.Contains(a.Id)).ToListAsync(cancellationToken);

            var user = accounts.FirstOrDefault()?.User;
            var userSubscription = user?.Subscriptions ?? new List<Data.Database.DbModels.Subscription>();

            var activeSubscription = _userAccountService.GetActiveSubscription(userSubscription);

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

                account.ConnectionStatus = operation.ConnectionStatus;
                account.AuthStatus = operation.AuthStatus;
                account.LogoutReason = operation.LogoutReason;

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

                    LogoutReason = operation.LogoutReason,
                    LogoutReasonText = operation.LogoutReason.GetInfo().Name,

                    ConnectionStatus = operation.ConnectionStatus,
                    ConnectionStatusText =  operation.ConnectionStatus.GetInfo().Name,

                    AuthStatus = operation.AuthStatus,
                    AuthStatusText = operation.AuthStatus.GetInfo().Name,

                    IsConnected = operation.AuthStatus == AccountAuthStatus.LoggedIn
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            //Inform app about the accounts status
            await _signalRService.NotifyAppAccountStatus(userAccountSignals, cancellationToken);

            //Send push notifications if the account is logged out
            foreach (var userAccountSignal in userAccountSignals)
            {
                var userId = userAccountSignal.AppId;
                var accountStatuses = userAccountSignal.AccountsStatus;

                foreach (var accountStatus in accountStatuses)
                {
                    if (accountStatus.AuthStatus == AccountAuthStatus.LoggedOut)
                    {
                        var message = $"{accountStatus.AccountName} has been logged out. Check your email for more details";
                        var isSubscriptionExpired = activeSubscription is null ? true : false;

                        _ =  _oneSignalService.PushLogoutNotificationAsync(userId.ToString(), message, isSubscriptionExpired);
                    }
                }
            }

            //Send Email
            var userAccounts = userAccountSignals.SelectMany(a => a.AccountsStatus).ToList();
            var loggedOutAccounts = new List<AccountStatusSignalRModel>();

            foreach (var userAccountSignal in userAccounts)
            {
                if (userAccountSignal.AuthStatus == AccountAuthStatus.LoggedOut)
                {
                    loggedOutAccounts.Add(userAccountSignal);
                }
            }
            if (user is not null)
            {
                _=_emailService.SendAccountLogoutEmailAsync(user.Email, user.Name, loggedOutAccounts);
            }

            return BaseResponse<UpdateAccountStatusModelResponse>.Success("Account status updated successfully", new UpdateAccountStatusModelResponse());
        }
    }
}
