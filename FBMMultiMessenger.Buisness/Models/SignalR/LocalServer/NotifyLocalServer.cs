namespace FBMMultiMessenger.Buisness.Models.SignalR.LocalServer
{
    //This class is responsible for notifying our local server that user has send a message from our app.
    public class NotifyLocalServer
    {
        public int ChatId { get; set; }
        public string FbChatId { get; set; } = string.Empty;
        public string FbAccountId { get; set; } = string.Empty;
        public int? AccountId { get; set; }
        public string Message { get; set; } = null!;
        public string OfflineUniqueId { get; set; } = string.Empty;

        public List<string> MediaPaths { get; set; } = new List<string>();
        public bool IsMessageFromApp { get; set; }
    }
}
