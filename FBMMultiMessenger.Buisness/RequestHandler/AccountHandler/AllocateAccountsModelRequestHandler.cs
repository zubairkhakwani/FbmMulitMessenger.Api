using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.Account;
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

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class AllocateAccountsModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, IUserAccountService _userAccountService, IHubContext<ChatHub> _hubContext) : IRequestHandler<AllocateAccountsModelRequest, BaseResponse<List<AllocateAccountsModelResponse>>>
    {
        public async Task<BaseResponse<List<AllocateAccountsModelResponse>>> Handle(AllocateAccountsModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return BaseResponse<List<AllocateAccountsModelResponse>>.Error("Invalid Request, please login again to continue");
            }

            var isParsed = Enum.TryParse<Roles>(currentUser.Role, out var userRole);


            var currentUserId = currentUser.Id;
            var localServer = await _dbContext.LocalServers.FirstOrDefaultAsync(ls => ls.UserId == currentUserId && ls.UniqueId == request.LocalServerId, cancellationToken);

            if (localServer is null)
            {
                return BaseResponse<List<AllocateAccountsModelResponse>>.Error("Local server not registered.");
            }

            List<Account> accountsToAllocate;

            if (userRole == Roles.SuperServer)
            {
                // For SuperServer: Get accounts from users who have active subscriptions with CanRunOnOurServer = true
                var eligibleUsers = await _dbContext.Users
                    .Include(u => u.Accounts)
                    .Include(u => u.Subscriptions)
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
                    .Where(a => a.LocalServerId == null)
                    .Take(request.Count)
                    .ToList();
            }
            else
            {
                // For other roles: Get accounts of the current user only
                accountsToAllocate = await _dbContext.Users
                                                     .Where(u => u.Id == currentUserId)
                                                     .Include(u => u.Accounts)
                                                     .SelectMany(u => u.Accounts)
                                                     .Where(a => a.LocalServerId == null)
                                                     .Take(request.Count)
                                                     .ToListAsync(cancellationToken);
            }

            var accountStatusSignalR = new AccountsStatusSignalRModel();

            foreach (var account in accountsToAllocate)
            {
                account.LocalServerId = localServer.Id;
                account.Status = AccountStatus.InProgress;

                // Prepare SignalR data
                accountStatusSignalR.AccountStatus.Add(account.Id, AccountStatusExtension.GetInfo(AccountStatus.InProgress).Name);
            }

            localServer.ActiveBrowserCount = accountsToAllocate.Count;
            await _dbContext.SaveChangesAsync(cancellationToken);


            var responseData = accountsToAllocate.Select(a => new AllocateAccountsModelResponse
            {
                Id = a.Id,
                Name = a.Name,
                Cookie = a.Cookie,
                DefaultMessage = a.DefaultMessage?.Message,
                CreatedAt = a.CreatedAt,

            }).ToList();

            await _hubContext.Clients.Group($"App_{currentUserId}")
                        .SendAsync("HandleAccountStatus", accountStatusSignalR, cancellationToken);

            return BaseResponse<List<AllocateAccountsModelResponse>>.Success("Accounts allocated successfully.", responseData);
        }
    }
}
