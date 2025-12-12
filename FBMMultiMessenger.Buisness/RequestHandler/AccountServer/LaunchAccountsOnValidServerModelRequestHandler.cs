using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.AccountServer;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountServer
{
    internal class LaunchAccountsOnValidServerModelRequestHandler(ApplicationDbContext _dbContext, IUserAccountService _userAccountService, ILocalServerService _localServerService, IHubContext<ChatHub> _hubContext) : IRequestHandler<LaunchAccountsOnValidServerModelRequest, BaseResponse<LaunchAccountsOnValidServerModelResponse>>
    {
        public async Task<BaseResponse<LaunchAccountsOnValidServerModelResponse>> Handle(LaunchAccountsOnValidServerModelRequest request, CancellationToken cancellationToken)
        {
            var accountsToLaunch = request.AccountsToLaunch;

            if (accountsToLaunch is null || accountsToLaunch.Count == 0)
            {
                return BaseResponse<LaunchAccountsOnValidServerModelResponse>.Error("No accounts to launch");
            }



            var serverAccountAssignments = new Dictionary<string, List<AccountDTO>>();
            var successfulAccounts = new List<int>();
            var failedAccounts = new List<(int AccountId, string Reason)>();

            foreach (var account in accountsToLaunch)
            {
                try
                {
                    var activeSubscription = _userAccountService.GetActiveSubscription(account.User.Subscriptions)
                        ?? _userAccountService.GetLastActiveSubscription(account.User.Subscriptions);

                    if (activeSubscription is null)
                    {
                        failedAccounts.Add((account.Id, "No active or recent subscription found"));
                        continue;
                    }

                    Data.Database.DbModels.LocalServer? assignedServer = null;

                    // Try super servers first if subscription allows
                    if (activeSubscription.CanRunOnOurServer)
                    {
                        var superServers = _dbContext.LocalServers.Where(ls => ls.IsActive && ls.IsSuperServer).ToList();

                        var powerfulSuperServers = _localServerService.GetPowerfulServers(superServers);

                        if (powerfulSuperServers?.Count > 0)
                        {
                            assignedServer = TryAssignToServer(powerfulSuperServers);
                        }
                    }

                    // Fall back to user's own servers
                    if (assignedServer is null)
                    {
                        var userServers = account.User.LocalServers;
                        var userPowerfulServers = _localServerService.GetPowerfulServers(userServers);

                        if (userPowerfulServers?.Count > 0)
                        {
                            assignedServer = TryAssignToServer(userPowerfulServers);
                        }
                    }

                    // If we found a server, assign the account
                    if (assignedServer is not null)
                    {
                        account.Status = AccountStatus.InProgress;
                        account.LocalServerId = assignedServer.Id;
                        assignedServer.ActiveBrowserCount++;

                        var accountDTO = new AccountDTO
                        {
                            Id = account.Id,
                            Name = account.Name,
                            Cookie = account.Cookie,
                            CreatedAt = DateTime.UtcNow,
                        };

                        if (!serverAccountAssignments.ContainsKey(assignedServer.UniqueId))
                        {
                            serverAccountAssignments[assignedServer.UniqueId] = new List<AccountDTO>();
                        }

                        serverAccountAssignments[assignedServer.UniqueId].Add(accountDTO);

                        successfulAccounts.Add(account.Id);
                    }
                    else
                    {
                        failedAccounts.Add((account.Id, "No available server slots"));
                    }
                }
                catch (Exception ex)
                {
                    failedAccounts.Add((account.Id, $"Error: {ex.Message}"));
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);




            // Send assignments to servers via SignalR
            foreach (var (uniqueId, accounts) in serverAccountAssignments)
            {
                try
                {
                    //here unique id is the identifier that tells signalR Connection
                    await _hubContext.Clients.Group($"{uniqueId}")
                        .SendAsync("HandleImportAccounts", accounts, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to notify server {uniqueId}: {ex.Message}");
                }
            }

            var response = new LaunchAccountsOnValidServerModelResponse
            {
                SuccessfulAccountIds = successfulAccounts,
                FailedAccounts = failedAccounts.Select(f => new FailedAccountInfo
                {
                    AccountId = f.AccountId,
                    Reason = f.Reason
                }).ToList()
            };

            if (failedAccounts.Count == accountsToLaunch.Count)
            {
                return BaseResponse<LaunchAccountsOnValidServerModelResponse>.Error("Failed to launch any accounts", result: response);

            }

            var message = failedAccounts.Count > 0
                ? $"Launched {successfulAccounts.Count} accounts, {failedAccounts.Count} failed"
                : $"Successfully launched all {successfulAccounts.Count} accounts";

            return BaseResponse<LaunchAccountsOnValidServerModelResponse>.Success(message, response);
        }


        #region Helper Methods

        private static Data.Database.DbModels.LocalServer? TryAssignToServer(List<Data.Database.DbModels.LocalServer> servers)
        {
            return servers
                .Where(s => s.ActiveBrowserCount < s.MaxBrowserCapacity)
                .OrderBy(s => s.ActiveBrowserCount)
                .FirstOrDefault();
        }

        #endregion
    }
}
