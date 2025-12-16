using AutoMapper;
using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Helpers;
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
    public class UpsertAccountModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, IUserAccountService _userAccountService, ISubscriptionServerProviderService _subscriptionServerProviderService, ILocalServerService _localServerService, IHubContext<ChatHub> _hubContext, IMapper _mapper) : IRequestHandler<UpsertAccountModelRequest, BaseResponse<UpsertAccountModelResponse>>
    {
        public async Task<BaseResponse<UpsertAccountModelResponse>> Handle(UpsertAccountModelRequest request, CancellationToken cancellationToken)
        {
            (bool isValid, int? currentUserId, string? fbAccountId, string errorMessage) = ValidateRequest(request.Cookie, cancellationToken);

            if (!isValid)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error(errorMessage);
            }

            request.UserId = currentUserId!.Value;

            if (request.AccountId is null)
            {
                return await AddRequestAsync(request, fbAccountId!, cancellationToken);
            }

            return await UpdateRequestAsync(request, fbAccountId!, cancellationToken);
        }

        private async Task<BaseResponse<UpsertAccountModelResponse>> AddRequestAsync(UpsertAccountModelRequest request, string fbAccountId, CancellationToken cancellationToken)
        {
            try
            {
                var logMessages = new List<string>();
                logMessages.Add($"Adding new account");

                var user = await _dbContext.Users
                                       .Include(p => p.Proxies)
                                       .Include(a => a.Accounts)
                                       .Include(p => p.VerificationTokens)
                                       .Include(s => s.Subscriptions)
                                       .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken: cancellationToken);

                if (user is null)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("We couldn’t find your account. Please create an account to continue.");
                }

                var userSubscriptions = user.Subscriptions;

                if (!userSubscriptions.Any())
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("Oh Snap, Looks like you don't have any subscription yet.", redirectToPackages: true);
                }

                var activeSubscription = _userAccountService.GetActiveSubscription(userSubscriptions);

                var response = new UpsertAccountModelResponse();

                if (activeSubscription is null || _userAccountService.IsSubscriptionExpired(activeSubscription))
                {
                    response.IsSubscriptionExpired = true;
                    return BaseResponse<UpsertAccountModelResponse>.Error("Oops! Your subscription has expired. Kindly renew your plan to continue adding accounts.", redirectToPackages: true, response);
                }

                var isLimitReaced = _userAccountService.HasLimitExceeded(activeSubscription);

                if (isLimitReaced)
                {
                    response.IsLimitExceeded = true;
                    return BaseResponse<UpsertAccountModelResponse>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.", result: response);
                }

                var userAccounts = user.Accounts;
                var isFbAccountAlreadyExist = userAccounts.Any(x => x.FbAccountId == fbAccountId);

                if (isFbAccountAlreadyExist)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("This account is already being used, please provide another valid facebook cookie.");
                }

                if (activeSubscription.CanRunOnOurServer && string.IsNullOrWhiteSpace(request.ProxyId))
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("Please provide proxy when adding account");
                }

                int? proxyId = null;

                if (!string.IsNullOrWhiteSpace(request.ProxyId))
                {
                    var userProxies = user.Proxies;

                    proxyId = string.IsNullOrWhiteSpace(request.ProxyId) ? null : Convert.ToInt32(request.ProxyId); ;

                    var isValidUserProxy = userProxies.Any(p => p.Id == proxyId);

                    if (!isValidUserProxy)
                    {
                        return BaseResponse<UpsertAccountModelResponse>.Error("Proxy does not exist, please provide valid proxy.");
                    }
                }

                if (!user.IsEmailVerified)
                {
                    var emailVerificationResponse = await _userAccountService.ProcessEmailVerificationAsync(user, cancellationToken);

                    return _mapper.Map<BaseResponse<UpsertAccountModelResponse>>(emailVerificationResponse);
                }

                var elegibleServers = await _subscriptionServerProviderService.GetEligibleServersAsync(activeSubscription);
                var powerfullEligibleServer = _localServerService.GetPowerfulServers(elegibleServers);
                var assignedServer = _localServerService.GetLeastLoadedServer(powerfullEligibleServer);

                var newAccount = new Account()
                {
                    UserId = request.UserId,
                    Cookie = request.Cookie,
                    Name = request.Name,
                    FbAccountId = fbAccountId,
                    ConnectionStatus = assignedServer is null ? AccountConnectionStatus.Offline : AccountConnectionStatus.Starting,
                    AuthStatus = AccountAuthStatus.Idle,
                    LocalServerId = assignedServer?.Id,
                    ProxyId = proxyId,
                    CreatedAt = DateTime.UtcNow
                };

                activeSubscription.LimitUsed++;

                if (assignedServer is not null)
                {
                    assignedServer.ActiveBrowserCount++;
                }

                await _dbContext.Accounts.AddAsync(newAccount, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);


                if (assignedServer is not null)
                {
                    // Inform our local server to open a new browser instance.
                    var newAccountHttpResponse = new AccountDTO()
                    {
                        Id =  newAccount.Id,
                        Name = newAccount.Name,
                        Cookie = newAccount.Cookie,
                        CreatedAt = newAccount.CreatedAt
                    };

                    await _hubContext.Clients.Group($"{assignedServer.UniqueId}")
                       .SendAsync("HandleUpsertAccount", newAccountHttpResponse, cancellationToken);
                }

                logMessages.Add($"Assigned Server Found: {assignedServer is not null}");
                logMessages.Add($"Assigned Server UniqueId : {assignedServer?.UniqueId}");
                WriteLog(logMessages, "Adding acount");

                return BaseResponse<UpsertAccountModelResponse>.Success("Account created successfully", response);
            }
            catch (Exception ex)
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }

                var fileName = $"Logs\\Add-Account-Error-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
                File.WriteAllText(fileName, string.Join(Environment.NewLine, $"Exception Message: {ex.Message}\n Inner Exception: {ex.InnerException}"));

                return BaseResponse<UpsertAccountModelResponse>.Error("Something went wrong, pleas try later");
            }
        }

        private async Task<BaseResponse<UpsertAccountModelResponse>> UpdateRequestAsync(UpsertAccountModelRequest request, string fbAccountId, CancellationToken cancellationToken)
        {
            var account = await _dbContext.Accounts
                                          .Include(u => u.User)
                                          .ThenInclude(ls => ls.LocalServers)
                                          .Include(u => u.User)
                                          .ThenInclude(p => p.Proxies)
                                          .Include(u => u.User)
                                          .ThenInclude(s => s.Subscriptions)
                                          .FirstOrDefaultAsync(x => x.Id == request.AccountId
                                                               &&
                                                               x.UserId == request.UserId, cancellationToken);

            if (account is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Account does not exist.");
            }

            var user = account.User;

            var activeSubscription = _userAccountService.GetActiveSubscription(user.Subscriptions) ?? _userAccountService.GetLastActiveSubscription(user.Subscriptions);

            if (activeSubscription is not null && activeSubscription.CanRunOnOurServer && string.IsNullOrWhiteSpace(request.ProxyId))
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Please provide proxy when editing account");
            }

            int? proxyId = null;

            if (!string.IsNullOrWhiteSpace(request.ProxyId))
            {
                var userProxies = user.Proxies;

                proxyId = string.IsNullOrWhiteSpace(request.ProxyId) ? null : Convert.ToInt32(request.ProxyId); ;

                var isValidUserProxy = userProxies.Any(p => p.Id == proxyId);

                if (!isValidUserProxy)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("Proxy does not exist, please provide valid proxy.");
                }
            }

            if (!user.IsEmailVerified)
            {
                var emailVerificationResponse = await _userAccountService.ProcessEmailVerificationAsync(user, cancellationToken);

                return _mapper.Map<BaseResponse<UpsertAccountModelResponse>>(emailVerificationResponse);
            }

            var newCookie = request.Cookie.Trim();
            var previousCookie = account.Cookie.Trim();

            var newAccountName = request.Name.Trim();
            var previousAccountName = account.Name.Trim();

            var newProxyId = proxyId;
            var previousProxyId = account.ProxyId;

            var shouldUpdate = newCookie != previousCookie || newAccountName != previousAccountName ||  newProxyId != previousProxyId;

            if (shouldUpdate)
            {
                var isCookieChanged = newCookie != previousCookie;

                //on which the account is running
                var accountLocalServer = account.LocalServer;

                account.Name = newAccountName;
                account.Cookie = newCookie;
                account.FbAccountId = fbAccountId;
                account.ProxyId = proxyId;
                account.UpdatedAt = DateTime.UtcNow;

                _dbContext.Accounts.Update(account);
                await _dbContext.SaveChangesAsync(cancellationToken);

                if (isCookieChanged && accountLocalServer is not null)
                {
                    //Inform our app to update the account status.
                    var accountsStatusModel = new List<AccountStatusSignalRModel>();
                    accountsStatusModel.Add(new AccountStatusSignalRModel() { AccountId = account.Id, ConnectionStatus = AccountConnectionStatus.Starting.GetInfo().Name });

                    await _hubContext.Clients.Group($"App_{request.UserId}")
                      .SendAsync("HandleAccountStatus", accountsStatusModel, cancellationToken);
                }

                if (accountLocalServer is not null)
                {
                    //Inform our local server to close/re-open browser accordingly.
                    var newAccountHttpResponse = new AccountDTO()
                    {
                        Id =  account.Id,
                        Name = account.Name,
                        Cookie = account.Cookie,
                        CreatedAt = account.CreatedAt,
                        IsCookieChanged = newCookie != previousCookie,
                    };

                    await _hubContext.Clients.Group($"{accountLocalServer.UniqueId}")
                   .SendAsync("HandleUpsertAccount", newAccountHttpResponse, cancellationToken);
                }
            }

            return BaseResponse<UpsertAccountModelResponse>.Success("Account updated successfully", new UpsertAccountModelResponse());
        }


        #region Helper Method
        private (bool isValid, int? currentUserId, string? fbAccountIda, string errorMessage) ValidateRequest(string cookie, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra Safety Check, if user has came here he would be logged in hence the current user will never be null.
            if (currentUser is null)
            {
                return (false, null, null, "Invalid request, Please login again to continue.");
            }

            (bool IsValidCookie, string? fbAccountId) = FBCookieValidatior.Validate(cookie);

            if (!IsValidCookie || string.IsNullOrWhiteSpace(fbAccountId))
            {

                return (false, null, null, "The cookie you provided is not valid. Please provide a valid facebook cookie.");
            }

            return (true, currentUser.Id, fbAccountId, "Validation Successful");
        }

        private void WriteLog(List<string> messages, string status)
        {
            try
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }

                var fileName = $"Logs\\{status}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
                File.WriteAllText(fileName, string.Join(Environment.NewLine, messages));
            }
            catch
            {
                // Ignore logging errors
            }
        }

        #endregion
    }
}
