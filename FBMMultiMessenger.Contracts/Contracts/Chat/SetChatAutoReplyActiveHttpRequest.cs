namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class SetChatAutoReplyActiveHttpRequest
    {
        public bool IsAutoReplyActive { get; set; }
    }

    public class SetChatAutoReplyActiveHttpResponse
    {
        public int ChatId { get; set; }
        public bool IsAutoReplyActive { get; set; }
    }
}
