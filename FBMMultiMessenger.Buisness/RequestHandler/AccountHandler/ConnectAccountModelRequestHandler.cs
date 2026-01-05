using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class ConnectAccountModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, ISubscriptionServerProviderService _subscriptionServerProviderService, ILocalServerService _localServerService, IUserAccountService _accountService, IHubContext<ChatHub> _hubContext) : IRequestHandler<ConnectAccountModelRequest, BaseResponse<object>>
    {
        public async Task<BaseResponse<object>> Handle(ConnectAccountModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            var currentUserId = currentUser!.Id;

            var account = await _dbContext.Accounts
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

            if ((accountLocalServer is not null &&  account.AuthStatus == AccountAuthStatus.LoggedIn))
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
                return BaseResponse<object>.Error("Unable to launch account.");
            }

            var newAccountHttpResponse = new AccountDTO()
            {
                Id =  account.Id,
                Name = account.Name,
                Cookie = account.Cookie,
                IsBrowserOpenRequest = true,
                CreatedAt = account.CreatedAt
            };

            account.ConnectionStatus = AccountConnectionStatus.Starting;
            account.AuthStatus = AccountAuthStatus.Idle;
            account.LocalServerId = assignedServer.Id;
            assignedServer.ActiveBrowserCount++;

            await _dbContext.SaveChangesAsync(cancellationToken);

            //Inform local server to open browser if not opened.
            await _hubContext.Clients.Group($"{assignedServer.UniqueId}")
                    .SendAsync("HandleAccountConnect", newAccountHttpResponse, cancellationToken);

            return BaseResponse<object>.Success("Please wait, account is being connected.", new object());
        }
    }
}
