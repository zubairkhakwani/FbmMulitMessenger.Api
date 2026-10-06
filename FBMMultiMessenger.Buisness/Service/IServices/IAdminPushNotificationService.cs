namespace FBMMultiMessenger.Buisness.Service.IServices
{
    public interface IAdminPushNotificationService
    {
        Task NotifyPaymentProofSubmittedAsync(
            string userName,
            string email,
            string billingCycle,
            decimal purchasedPrice,
            CancellationToken cancellationToken = default);
    }
}
