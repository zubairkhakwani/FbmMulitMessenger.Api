using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Data.Database.DbModels;
using Microsoft.Extensions.Configuration;
using OneSignal.RestAPIv3.Client;
using OneSignal.RestAPIv3.Client.Resources.Notifications;
using Org.BouncyCastle.Tls;

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

        public async Task SendMessageNotification(string userId, string message, string senderName, int chatId)
        {
            var client = new OneSignalClient(_restApiKey);
            var externalId = $"FBM_{userId}";

            var category = NotificationCategory.Chat.ToString();

            var deepLinkUrl = $"myapp://chat?category={category}&isNotification=true&chatId={chatId}&message=${message}";

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
                Data = new Dictionary<string, string>
                {
                    { "category", category },
                    { "chatId", chatId.ToString() },
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


        public async Task PushLogoutNotificationAsync(string userId, string message, string accountId)
        {
            var client = new OneSignalClient(_restApiKey);
            var externalId = $"FBM_{userId}";

            var category = NotificationCategory.Account.ToString();

            var deepLinkUrl = $"myapp://account?category={category}&isNotification=true&accountId={accountId}";

            var options = new NotificationCreateOptions
            {
                AppId = Guid.Parse(_appId),
                IncludeExternalUserIds = new List<string>() { externalId },
                Headings = new Dictionary<string, string>
                {
                    { "en", "FBM Messenger" }
                },
                Contents = new Dictionary<string, string>
                {
                    { "en", message },
                },
                Data = new Dictionary<string, string>
                {
                    { "category", category },
                    { "message",message},
                    { "accountId",accountId},
                    { "type", "new_message" }
                },
                Url = deepLinkUrl,
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



        public async Task ProxyNotWorkingNotificationAsync(int userId, string message)
        {
            var client = new OneSignalClient(_restApiKey);
            var externalId = $"FBM_{userId}";

            var category = NotificationCategory.Proxy.ToString();

            var deepLinkUrl = $"myapp://proxy?category={category}&isNotification=true";

            var options = new NotificationCreateOptions
            {
                AppId = Guid.Parse(_appId),
                IncludeExternalUserIds = new List<string>() { externalId },
                Headings = new Dictionary<string, string>
                {
                    { "en", "FBM Messenger" }
                },
                Contents = new Dictionary<string, string>
                {
                    { "en", message },
                },
                Data = new Dictionary<string, string>
                {
                    { "category", category },
                    { "message",message},
                    { "type", "new_message" }
                },
                Url = deepLinkUrl,
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
