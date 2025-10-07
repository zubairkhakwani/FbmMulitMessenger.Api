using Microsoft.Extensions.Configuration;
using OneSignal.RestAPIv3.Client;
using OneSignal.RestAPIv3.Client.Resources.Notifications;

namespace FBMMultiMessenger.Buisness.OneSignal
{
    public class OneSignalNotificationService
    {
        private readonly string _appId;
        private readonly string _restApiKey;

        public OneSignalNotificationService(IConfiguration configuration)
        {
            _appId =  configuration.GetValue<string>("OneSignal:AppId")!;
            _restApiKey =  configuration.GetValue<string>("OneSignal:ApiKey")!;
        }
        public async Task SendMessageNotification(string userId, string message, string senderName, string chatId, bool isSubscriptionExpired = false)
        {
            var client = new OneSignalClient(_restApiKey);

            var options = new NotificationCreateOptions
            {
                AppId = Guid.Parse(_appId),
                IncludeExternalUserIds = new List<string> { userId },
                Headings = new Dictionary<string, string>
                {
                    { "en", $"New message from {senderName}" }
                },
                Contents = new Dictionary<string, string>
                {
                    { "en", message }
                },
                Data = new Dictionary<string, string>
                {
                    { "chatId", chatId },
                    { "isSubscriptionExpired", isSubscriptionExpired.ToString() },
                    {"message",message },
                    { "type", "new_message" }
                }
            };

            try
            {
                var result = await client.Notifications.CreateAsync(options);
                Console.WriteLine($"Notification sent: {result.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }
    }
}
