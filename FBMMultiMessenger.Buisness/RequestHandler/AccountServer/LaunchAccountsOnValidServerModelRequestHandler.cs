using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
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

            var serverAccountAssignments = new Dictionary<string, List<LocalServerAccountDTO>>();
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
                        var superServers = _dbContext.LocalServers.Where(ls => ls.IsActive && ls.IsOnline && ls.IsSuperServer).ToList();

                        var powerfulSuperServers = _localServerService.GetPowerfulServers(superServers);
                        var leastUsedServer = _localServerService.GetLeastLoadedServer(powerfulSuperServers);

                        if (leastUsedServer is not null)
                        {
                            assignedServer = leastUsedServer;
                        }
                    }

                    // If subscription does not allow use user's own servers
                    else
                    {
                        var userServers = account.User.LocalServers.Where(us => us.IsActive && us.IsOnline).ToList();
                        var userPowerfulServers = _localServerService.GetPowerfulServers(userServers);

                        var leastUsedServer = _localServerService.GetLeastLoadedServer(userPowerfulServers);

                        if (leastUsedServer is not null)
                        {
                            assignedServer = leastUsedServer;
                        }
                    }

                    // If we found a server, assign the account
                    if (assignedServer is not null)
                    {
                        account.ConnectionStatus = AccountConnectionStatus.Starting;
                        account.LocalServerId = assignedServer.Id;
                        assignedServer.ActiveBrowserCount++;

                        var accountDTO = new LocalServerAccountDTO
                        {
                            Id = account.Id,
                            Name = account.Name,
                            Cookie = account.Cookie,
                            CreatedAt = DateTime.UtcNow,
                        };

                        if (!serverAccountAssignments.ContainsKey(assignedServer.UniqueId))
                        {
                            serverAccountAssignments[assignedServer.UniqueId] = new List<LocalServerAccountDTO>();
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
    }
}
