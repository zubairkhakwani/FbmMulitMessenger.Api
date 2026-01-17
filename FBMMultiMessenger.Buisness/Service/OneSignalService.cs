using Microsoft.Extensions.Configuration;
using OneSignal.RestAPIv3.Client;
using OneSignal.RestAPIv3.Client.Resources.Notifications;

namespace FBMMultiMessenger.Buisness.Service
{
    public class OneSignalService
    {
        private readonly string _appId;
        private readonly string _restApiKey;

        public OneSignalService(IConfiguration configuration)
        {
            _appId =  configuration.GetValue<string>("OneSignal:AppId")!;
            _restApiKey =  configuration.GetValue<string>("OneSignal:ApiKey")!;
        }

        public async Task SendMessageNotification(string userId, string message, string senderName, string chatId, bool isSubscriptionExpired = false)
        {
            var client = new OneSignalClient(_restApiKey);
            var externalId = $"FBM_{userId}";

            var deepLinkUrl = isSubscriptionExpired
                                                  ? $"myapp://packages?isExpired=true&message={Uri.EscapeDataString(message)}"
                                                  : $"myapp://chat?isNotification=true&fbChatId={chatId}&isSubscriptionExpired=false";

            var options = new NotificationCreateOptions
            {
                AppId = Guid.Parse(_appId),
                IncludeExternalUserIds = new List<string>() { externalId },
                Headings = new Dictionary<string, string>
                {
                    { "en", $"{senderName}" }
                },
                Contents = new Dictionary<string, string>
                {
                    { "en", message },
                },
                Subtitle = new Dictionary<string, string>
                {
                    { "en", "Iam subtitle" },
                },
                Data = new Dictionary<string, string>
                {
                    { "chatId", chatId },
                    { "isSubscriptionExpired", isSubscriptionExpired.ToString() },
                    { "message",message},
                    { "type", "new_message" }
                },
                Url = deepLinkUrl,
                CollapseId = chatId.ToString(),
            };

            try
            {
                var result = await client.Notifications.CreateAsync(options);
            }
            catch (Exception ex)
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }
                var fileName = $"Logs\\Error-Push-Notifiction-{DateTime.Now:yyyy-MM-dd-HH-mm}.txt";
                File.WriteAllText(fileName, string.Join(Environment.NewLine, $"External User Id=> {userId}", $"Exception => {ex.Message}", $"InnerException => {ex.InnerException}", $"Full Error => {ex}"));
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }
    }
}
