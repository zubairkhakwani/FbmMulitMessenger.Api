using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Extensions;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.LocalServer
{
    internal class GetLocalServerAccountsModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, IUserAccountService _userAccountService, IHubContext<ChatHub> _hubContext) : IRequestHandler<GetLocalServerAccountsModelRequest, BaseResponse<List<GetLocalServerAccountsModelResponse>>>
    {
        public async Task<BaseResponse<List<GetLocalServerAccountsModelResponse>>> Handle(GetLocalServerAccountsModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return BaseResponse<List<GetLocalServerAccountsModelResponse>>.Error("Invalid Request, please login again to continue");
            }

            _= Enum.TryParse<Roles>(currentUser.Role, out var userRole);

            var currentUserId = currentUser.Id;
            var localServer = await _dbContext.LocalServers.FirstOrDefaultAsync(ls => ls.UserId == currentUserId && ls.UniqueId == request.LocalServerId, cancellationToken);

            if (localServer is null)
            {
                return BaseResponse<List<GetLocalServerAccountsModelResponse>>.Error("Local server not registered.");
            }

            List<Account> accountsToAllocate;

            if (userRole == Roles.SuperServer)
            {
                // For SuperServer: Get accounts from users who have active subscriptions with CanRunOnOurServer = true
                var eligibleUsers = await _dbContext.Users
                    .Include(u => u.Accounts)
                    .ThenInclude(p => p.Proxy)
                    .Include(u => u.Subscriptions)
                    .Include(a => a.Accounts)
                    .ThenInclude(dm => dm.DefaultMessage)
                    .ToListAsync(cancellationToken);

                accountsToAllocate = eligibleUsers
                    .Where(u => u.Subscriptions != null && u.Subscriptions.Any())
                    .Select(u => new
                    {
                        User = u,
                        ActiveSubscription = _userAccountService.GetLastActiveSubscription(u.Subscriptions)
                    })
                    .Where(x => x.ActiveSubscription != null && x.ActiveSubscription.CanRunOnOurServer)
                    .SelectMany(x => x.User.Accounts ?? Enumerable.Empty<Account>())
                    .Where(a => a.IsActive && (a.LocalServerId == null || a.LocalServerId == localServer.Id))
                    .OrderByDescending(a => a.LocalServerId == localServer.Id)
                    .Take(request.Limit)
                    .ToList();
            }

            else
            {
                // For other roles: Get accounts of the current user only
                accountsToAllocate = await _dbContext.Users
                                                     .Where(u => u.Id == currentUserId)
                                                     .Include(u => u.Accounts)
                                                     .ThenInclude(p => p.Proxy)
                                                     .Include(u => u.Accounts)
                                                     .ThenInclude(dm => dm.DefaultMessage)
                                                     .SelectMany(u => u.Accounts)
                                                     .Where(a => a.IsActive && (a.LocalServerId == null || a.LocalServerId == localServer.Id))
                                                     .OrderByDescending(a => a.LocalServerId == localServer.Id)
                                                     .Take(request.Limit)
                                                     .ToListAsync(cancellationToken);
            }

            var accountStatusSignalR = new List<AccountStatusSignalRModel>();

            foreach (var account in accountsToAllocate)
            {
                account.LocalServerId = localServer.Id;
                account.ConnectionStatus = AccountConnectionStatus.Starting;
                account.AuthStatus = AccountAuthStatus.Idle;
                account.Reason = AccountReason.AssigningToLocalServer;

                // Prepare SignalR data
                accountStatusSignalR.Add(
                    new AccountStatusSignalRModel()
                    {
                        AccountId = account.Id,
                        AccountName = account.Name,
                        ConnectionStatus = AccountConnectionStatus.Starting,
                        ConnectionStatusText =  AccountConnectionStatus.Starting.GetInfo().Name,
                        AuthStatus = AccountAuthStatus.Idle,
                        AuthStatusText = AccountAuthStatus.Idle.GetInfo().Name,
                        Reason  = AccountReason.AssigningToLocalServer,
                        ReasonText = AccountReason.AssigningToLocalServer.GetInfo().Name,
                        IsConnected = false
                    }
                    );
            }

            localServer.ActiveBrowserCount = accountsToAllocate.Count;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var responseData = accountsToAllocate.Select(a => new GetLocalServerAccountsModelResponse
            {
                Id = a.Id,
                Name = a.Name,
                Cookie = a.Cookie,
                DefaultMessage = a.DefaultMessage?.Message,
                CreatedAt = a.CreatedAt,
                Proxy = a.Proxy == null ? null : new LocalServerAccountsProxyModelResponse()
                {
                    Id = a.Proxy.Id,
                    Ip_Port = a.Proxy.Ip_Port,
                    Name = a.Proxy.Name,
                    Password = a.Proxy.Password
                }

            }).ToList();

            await _hubContext.Clients.Group($"App_{currentUserId}")
                        .SendAsync("HandleAccountStatus", accountStatusSignalR, cancellationToken);

            return BaseResponse<List<GetLocalServerAccountsModelResponse>>.Success("Accounts allocated successfully.", responseData);
        }
    }
}
