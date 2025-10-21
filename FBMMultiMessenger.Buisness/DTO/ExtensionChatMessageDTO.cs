namespace FBMMultiMessenger.Buisness.DTO
{

    //This class is responsible for notifying our browser extension that user has send a message from our app.
    public class NotifyExtensionDTO
    {
        public int ChatId { get; set; }
        public string FbChatId { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string OfflineUniqueId { get; set; } = string.Empty;

        public List<string> MediaPaths { get; set; } = new List<string>();
        public bool IsMessageFromApp { get; set; }
    }
}
