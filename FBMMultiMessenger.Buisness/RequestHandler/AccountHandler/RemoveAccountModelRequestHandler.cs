using FBMMultiMessenger.Buisness.Models.SignalR.Server;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class RemoveAccountModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, IUserAccountService _userAccountService, IHubContext<ChatHub> _hubContext) : IRequestHandler<RemoveAcountModelRequest, BaseResponse<ToggleAcountStatusModelResponse>>
    {
        public async Task<BaseResponse<ToggleAcountStatusModelResponse>> Handle(RemoveAcountModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currenuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<ToggleAcountStatusModelResponse>.Error("Invalid Request, Please login again to continue.");
            }
            if (request.AccountIds.Count == 0)
            {
                return BaseResponse<ToggleAcountStatusModelResponse>.Error("Invalid Request, Please select any account to delete.");
            }

            var accounts = await _dbContext.Accounts
                                           .Include(ls => ls.LocalServer)
                                           .Include(u => u.User)
                                           .ThenInclude(s => s.Subscriptions)
                                           .Where(x => x.UserId == currentUser.Id
                                                  &&
                                                 request.AccountIds.Any(id => id == x.Id))
                                           .ToListAsync(cancellationToken);

            var accountsUser = accounts.FirstOrDefault()?.User;
            var userSubscriptions = accountsUser?.Subscriptions;

            if (userSubscriptions is null || userSubscriptions.Count == 0)
            {
                return BaseResponse<ToggleAcountStatusModelResponse>.Error("User has no active subscription.");
            }

            var activeSubscription = _userAccountService.GetActiveSubscription(userSubscriptions)
                                     ?? _userAccountService.GetLastActiveSubscription(userSubscriptions);

            if (activeSubscription is null)
            {
                return BaseResponse<ToggleAcountStatusModelResponse>.Error("User has no active subscription.");
            }

            activeSubscription.LimitUsed-= accounts.Count;

            //Mark accounts as soft delete and decrease active browser count on assigned local servers
            foreach (var account in accounts)
            {
                account.IsActive = false;

                var accountLocalServer = account.LocalServer;

                if (accountLocalServer is not null && accountLocalServer.ActiveBrowserCount > 0)
                {
                    accountLocalServer.ActiveBrowserCount--;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Group accounts by their assigned server
            var accountsByServer = accounts
                                        .Where(a => a.LocalServer is not null)
                                        .GroupBy(a => a.LocalServer!.UniqueId);

            var serverAccountDeletion = accountsByServer.Select(group => new ServerCloseAccountRequest
            {
                ServerId = group.Key,
                Accounts = group.Select(account => new AccountsCloseInfo
                {
                    Id = account.Id,
                    Name = account.Name,
                    Cookie = account.Cookie,
                    CreatedAt = account.CreatedAt,
                }).ToList(),
            }).ToList();


            //Inform each server about the account deletion via SignalR
            if (serverAccountDeletion is not null && serverAccountDeletion.Count > 0)
            {
                foreach (var server in serverAccountDeletion)
                {
                    await _hubContext.Clients.Group($"{server.ServerId}")
                                             .SendAsync("HandleAccountRemoval", server.Accounts, cancellationToken);
                }
            }

            var responseMessage = accounts.Count > 1 ? "Selected accounts" : "Account";

            return BaseResponse<ToggleAcountStatusModelResponse>.Success($"{responseMessage} has been deleted.", new ToggleAcountStatusModelResponse());
        }
    }
}
