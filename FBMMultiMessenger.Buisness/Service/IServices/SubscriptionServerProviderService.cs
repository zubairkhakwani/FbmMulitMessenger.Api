using FBMMultiMessenger.Data.Database.DbModels;

namespace FBMMultiMessenger.Buisness.Service.IServices
{
    public interface ISubscriptionServerProviderService
    {
        Task<List<LocalServer>?> GetEligibleServersAsync(Subscription userSubscription);
    }
}
