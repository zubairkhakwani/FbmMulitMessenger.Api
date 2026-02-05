using FBMMultiMessenger.Data.Database.DbModels;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public static class ChatMessagesHelper
    {
        public static MessagePreviewResult GetMessagePreview(MessagePreviewRequest request)
        {
            var senderName = request.IsReceived ? (request.FbListingTitle?.Split(" ")[0] ?? "") : "You";

            var messageCount = request.Messages.Count;

            var countText = $"{(messageCount > 1 ? messageCount : "a")}";
            var filesText = $"{(messageCount > 1 ? "s" : "")}";

            string messagePreview = request switch
            {
                { IsImageMessage: true } => $"sent {countText} photo{filesText}",
                { IsVideoMessage: true } => $"sent {countText} video{filesText}",
                { IsAudioMessage: true } => $"sent {countText} audio{filesText}",

                _ => request.Messages.FirstOrDefault()!
            };

            return new MessagePreviewResult() { MessagPreview = messagePreview, SenderName = senderName };
        }

        public static MessageReplyResult? GetMessageReply(MessageReplyRequest request)
        {
            var fbMessageReplyId = request.FbMessageReplyId;

            var chatMessages = request.ChatMessages;

            if (fbMessageReplyId is null) return null;

            var chatMessage = chatMessages.FirstOrDefault(cm => cm.FbMessageId == fbMessageReplyId);

            if (chatMessage is null) return null;

            var result = new MessageReplyResult();

            result.ReplyTo = chatMessage.IsReceived ? chatMessage.Chat.OtherUserName : "You";

            if (chatMessage.IsVideoMessage)
            {
                result.Message = "Video message";
            }
            else if (chatMessage.IsImageMessage)
            {
                result.Message = "Image message";
            }
            else if (chatMessage.IsAudioMessage)
            {
                result.Message = "Audio message";
            }
            result.Message = chatMessage.Message;

            return result;
        }
    }

    public class MessagePreviewRequest
    {
        public List<string> Messages { get; set; } = new();
        public bool IsReceived { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public string? FbListingTitle { get; set; }
    }

    public class MessagePreviewResult
    {
        public string MessagPreview { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
    }


    public class MessageReplyRequest
    {
        public string? FbMessageReplyId { get; set; }
        public List<ChatMessages> ChatMessages = new();
    }
    public class MessageReplyResult
    {
        public string Message { get; set; } = string.Empty;
        public string ReplyTo { get; set; } = string.Empty;
    }
}
