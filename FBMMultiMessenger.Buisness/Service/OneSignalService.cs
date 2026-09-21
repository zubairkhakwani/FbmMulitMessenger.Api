using FBMMultiMessenger.Contracts.Enums;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OneSignal.RestAPIv3.Client;
using OneSignal.RestAPIv3.Client.Resources.Notifications;

namespace FBMMultiMessenger.Buisness.Service
{
    public class OneSignalService
    {
        private readonly string _appId;
        private readonly string _restApiKey;

        /// <summary>Must match Flutter MainActivity CHANNEL_ID / res/raw file (no extension).</summary>
        private const string AndroidMessageChannelId = "fbm_messages";
        private const string AndroidSoundName = "notification_ding";
        private const string IosSoundFileName = "notification_ding.mp3";

        public OneSignalService(IConfiguration configuration)
        {
            _appId = configuration.GetValue<string>("OneSignal:AppId")!;
            _restApiKey = configuration.GetValue<string>("OneSignal:ApiKey")!;
        }

        /// <summary>
        /// Extends the stock options with <c>existing_android_channel_id</c>
        /// (custom ding channel created by the Flutter Android app).
        /// </summary>
        private sealed class FbmNotificationCreateOptions : NotificationCreateOptions
        {
            [JsonProperty("existing_android_channel_id")]
            public string ExistingAndroidChannelId { get; set; } = AndroidMessageChannelId;
        }

        private static void ApplyCustomSound(FbmNotificationCreateOptions options)
        {
            options.AndroidSound = AndroidSoundName;
            options.IosSound = IosSoundFileName;
            options.ExistingAndroidChannelId = AndroidMessageChannelId;
        }

        public async Task SendMessageNotification(string userId, string message, string senderName, int chatId)
        {
            var client = new OneSignalClient(_restApiKey);
            var externalId = $"FBM_{userId}";

            var category = NotificationCategory.Chat.ToString();

            var deepLinkUrl = $"myapp://chat?category={category}&isNotification=true&chatId={chatId}&message=${message}";

            var options = new FbmNotificationCreateOptions
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
                    { "message", message },
                    { "type", "new_message" }
                },
                Url = deepLinkUrl,
                CollapseId = chatId.ToString(),
            };
            ApplyCustomSound(options);

            try
            {
                await client.Notifications.CreateAsync(options);
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

            var options = new FbmNotificationCreateOptions
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
                    { "message", message },
                    { "accountId", accountId },
                    { "type", "new_message" }
                },
                Url = deepLinkUrl,
            };
            ApplyCustomSound(options);

            try
            {
                await client.Notifications.CreateAsync(options);
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

            var options = new FbmNotificationCreateOptions
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
                    { "message", message },
                    { "type", "new_message" }
                },
                Url = deepLinkUrl,
            };
            ApplyCustomSound(options);

            try
            {
                await client.Notifications.CreateAsync(options);
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

        /// <summary>
        /// Account slot limit hit (e.g. extension register). Opens Packages with upgrade pitch.
        /// </summary>
        public async Task PushAccountLimitExceededNotificationAsync(string userId, string message)
        {
            var client = new OneSignalClient(_restApiKey);
            var externalId = $"FBM_{userId}";

            var category = NotificationCategory.Subscription.ToString();

            var deepLinkUrl =
                $"myapp://pricing?category={category}&isNotification=true&isLimitExceeded=true&message={Uri.EscapeDataString(message)}";

            var options = new FbmNotificationCreateOptions
            {
                AppId = Guid.Parse(_appId),
                IncludeExternalUserIds = new List<string>() { externalId },
                Headings = new Dictionary<string, string>
                {
                    { "en", "Account limit reached" }
                },
                Contents = new Dictionary<string, string>
                {
                    { "en", message },
                },
                Data = new Dictionary<string, string>
                {
                    { "category", category },
                    { "message", message },
                    { "isLimitExceeded", "true" },
                    { "type", "account_limit" }
                },
                Url = deepLinkUrl,
                CollapseId = "account_limit",
            };
            ApplyCustomSound(options);

            try
            {
                await client.Notifications.CreateAsync(options);
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
