namespace FBMMultiMessenger.Buisness.DTO
{
    public class NotifyExtensionDTO
    {
        public int ChatId { get; set; }
        public string FbChatId { get; set; } = null!;
        public string Message { get; set; } = null!;
        public List<string> MediaPaths { get; set; } = new List<string>();
        public bool IsMessageFromApp { get; set; }
    }

    public class ExtensionChatMessageResponseDTO
    {
        public bool Success { get; set; }
        public NotifyExtensionDTO Data { get; set; } = new NotifyExtensionDTO();
        public string Message { get; set; } = null!;
    }
}
