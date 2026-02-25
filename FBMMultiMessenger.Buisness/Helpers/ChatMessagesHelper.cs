using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Data.Database.DbModels;
using System.Text.Json;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public static class ChatMessagesHelper
    {
        public static MessagePreviewResult GetMessagePreview(MessagePreviewRequest request)
        {
            var senderName = "You";

            if (request.IsReceived && !string.IsNullOrWhiteSpace(request.OtherUserName))
            {
                senderName = request.OtherUserName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            }

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

            var message = chatMessage.Message;

            if (chatMessage.IsVideoMessage || chatMessage.IsImageMessage)
            {
                var mediaUrls = JsonSerializer.Deserialize<List<string>>(message);

                result.Attachments = mediaUrls?.Select(x => new MessageReplyFileModelResponse()
                {
                    Url = x
                }).ToList();
            }

            if (chatMessage.IsVideoMessage)
            {
                result.Type = MessageReplyType.Video;
            }
            else if (chatMessage.IsImageMessage)
            {
                result.Type = MessageReplyType.Image;
            }
            else if (chatMessage.IsAudioMessage)
            {
                result.Type = MessageReplyType.Audio;
            }
            else
            {
                result.Type = MessageReplyType.Text;
            }

            result.Reply = message ?? string.Empty;

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
        public string? OtherUserName { get; set; }
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
        public string Reply { get; set; } = string.Empty;
        public string ReplyTo { get; set; } = string.Empty;
        public MessageReplyType Type { get; set; }
        public List<MessageReplyFileModelResponse>? Attachments { get; set; }
    }
}
