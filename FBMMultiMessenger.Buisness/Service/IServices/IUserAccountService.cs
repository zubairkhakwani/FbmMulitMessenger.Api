using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;

namespace FBMMultiMessenger.Buisness.Service.IServices
{
    public interface IUserAccountService
    {
        Task<BaseResponse<EmailVerificationResponse>> ProcessEmailVerificationAsync(User user, CancellationToken cancellationToken);

        Subscription? GetActiveSubscription(List<Subscription> subscriptions);
        Subscription? GetLastActiveSubscription(List<Subscription> subscriptions);

        bool HasLimitExceeded(Subscription subscription);

        bool IsSubscriptionExpired(Subscription subscription);
    }
}
