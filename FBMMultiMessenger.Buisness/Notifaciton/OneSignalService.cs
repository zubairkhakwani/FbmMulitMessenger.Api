using Microsoft.Extensions.Configuration;
using OneSignal.RestAPIv3.Client;
using OneSignal.RestAPIv3.Client.Resources.Notifications;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FBMMultiMessenger.Buisness.Notifaciton
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
            //var playerIds = await GetFilteredPlayerIdsByExternalUserId(userId);


            var client = new OneSignalClient(_restApiKey);

            var options = new NotificationCreateOptions
            {
                AppId = Guid.Parse(_appId),
                IncludeExternalUserIds = new List<string>() { userId },
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
                    { "message",message },
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

        public async Task<List<string>> GetFilteredPlayerIdsByExternalUserId(string externalUserId)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {_restApiKey}");

            var url = $"https://onesignal.com/api/v1/players?app_id={_appId}&external_user_id={externalUserId}";

            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var players = JsonSerializer.Deserialize<OneSignalPlayersResponse>(content);

                return players?.Players?
                    .Where(p => p.NotificationTypes > 0) // Only include subscribed devices
                    .Select(p => p.Id)
                    .ToList() ?? new List<string>();
            }

            return new List<string>();
        }

        public class OneSignalPlayersResponse
        {
            [JsonPropertyName("players")]
            public List<PlayerInfo> Players { get; set; }
        }

        public class PlayerInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("notification_types")]
            public int NotificationTypes { get; set; } // 1 = subscribed, -2 = unsubscribed
        }
    }
}
