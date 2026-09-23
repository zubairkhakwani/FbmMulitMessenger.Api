using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    public class RegisterFacebookAccountFromExtensionRequestHandler : IRequestHandler<RegisterFacebookAccountFromExtensionRequest, BaseResponse<RegisterFacebookAccountFromExtensionResponse>>
    {
        private readonly ApplicationDbContext dbContext;
        private readonly CurrentUserService currentUserService;
        private readonly OneSignalService oneSignalService;
        private readonly AccountActiveStatusCache accountActiveStatusCache;

        public RegisterFacebookAccountFromExtensionRequestHandler(
            ApplicationDbContext dbContext,
            CurrentUserService currentUserService,
            OneSignalService oneSignalService,
            AccountActiveStatusCache accountActiveStatusCache)
        {
            this.dbContext = dbContext;
            this.currentUserService = currentUserService;
            this.oneSignalService = oneSignalService;
            this.accountActiveStatusCache = accountActiveStatusCache;
        }

        public async Task<BaseResponse<RegisterFacebookAccountFromExtensionResponse>> Handle(RegisterFacebookAccountFromExtensionRequest request, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            const int maxRetries = 3;

            var currentUser = currentUserService.GetCurrentUser();

            while (retryCount < maxRetries)
            {
                try
                {
                    var user = await dbContext.Users
                                             .Include(u => u.Subscriptions)
                                             .FirstOrDefaultAsync(u => u.Id == currentUser!.Id, cancellationToken);

                    if (user == null)
                    {
                        return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Error("User not found.");
                    }

                    var dbAccount = await dbContext.Accounts
                                                   .FirstOrDefaultAsync(a => a.FbAccountId == request.FbAccountId && a.UserId == currentUser!.Id, cancellationToken);

                    if (dbAccount != null && dbAccount.IsActive)
                    {
                        if (dbAccount.IsExtensionConnected)
                        {
                            return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Error("Account is already connected, can not connect twice.", showSweetAlert: true);
                        }
                        else
                        {
                            accountActiveStatusCache.Set(dbAccount.Id, true);
                            return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Success("Account already registered.", new() { AccountId = dbAccount.Id });
                        }
                    }

                    var now = DateTime.UtcNow;

                    var activeSubscription = user.Subscriptions
                                         .Where(x =>
                                            x.StartedAt <= now
                                            &&
                                            x.ExpiredAt > now)
                                         .OrderByDescending(x => x.StartedAt)
                                         .FirstOrDefault();

                    if (activeSubscription == null)
                    {
                        return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Error("Oh Snap, Looks like you don't have any subscription yet.", showSweetAlert: true);
                    }

                    var maxLimit = activeSubscription.MaxLimit;
                    var limitUsed = activeSubscription.LimitUsed;

                    if (limitUsed >= maxLimit)
                    {
                        const string limitMessage =
                            "You’ve reached the maximum limit of your subscription plan. Please upgrade your plan from the app.";

                        // Fire-and-forget push so the mobile app can open Packages + pitch.
                        _ = oneSignalService.PushAccountLimitExceededNotificationAsync(
                            currentUser!.Id.ToString(),
                            limitMessage);

                        return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Error(
                            limitMessage,
                            showSweetAlert: true,
                            result: new RegisterFacebookAccountFromExtensionResponse
                            {
                                IsLimitExceeded = true,
                            });
                    }

                    activeSubscription.LimitUsed++;

                    if (dbAccount != null)
                    {
                        dbAccount.IsActive = true;
                        dbAccount.UpdatedAt = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync(cancellationToken);

                        accountActiveStatusCache.Set(dbAccount.Id, true);
                        return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Success("", new() { AccountId = dbAccount.Id });
                    }

                    var accountToAdd = new Account()
                    {
                        UserId = currentUser.Id,
                        Name = request.FbAccountId,
                        FbAccountId = request.FbAccountId,
                        IsActive = true,
                        ConnectionStatus = Contracts.Enums.AccountConnectionStatus.Starting,
                        AuthStatus = Contracts.Enums.AccountAuthStatus.Idle,
                        CreatedAt = DateTime.UtcNow,
                        Reason = Contracts.Enums.AccountReason.ConnectedWithExtension
                    };

                    dbContext.Accounts.Add(accountToAdd);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    accountActiveStatusCache.Set(accountToAdd.Id, true);
                    return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Success("", new() { AccountId = accountToAdd.Id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Error("System is busy, please try again.");
                    }

                    dbContext.ChangeTracker.Clear();

                    await Task.Delay(Random.Shared.Next(100, 300));
                }
                catch (Exception ex)
                {
                    return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Error("An error occured, please contant support team.");
                }
            }

            return BaseResponse<RegisterFacebookAccountFromExtensionResponse>.Error("System is busy, please try again.");
        }
    }
}
