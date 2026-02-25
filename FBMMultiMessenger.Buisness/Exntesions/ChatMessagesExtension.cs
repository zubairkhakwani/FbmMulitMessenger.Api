using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Data.Database.DbModels;
using System.Text.Json;

namespace FBMMultiMessenger.Buisness.Exntesions
{
    public static class ChatMessagesExtension
    {
        public static MessagePreviewRequest ToMessagePreviewRequest(this ChatMessages source, string fbListingTitle)
        {
            List<string> messages;

            if (string.IsNullOrWhiteSpace(source.Message))
            {
                messages = new List<string>();
            }
            else
            {
                if (source.Message.TrimStart().StartsWith("["))
                {
                    messages = JsonSerializer.Deserialize<List<string>>(source.Message)
                               ?? new List<string> { source.Message };
                }
                else
                {
                    messages = new List<string> { source.Message };
                }
            }
            return new MessagePreviewRequest()
            {
                OtherUserName = fbListingTitle,
                IsImageMessage = source.IsImageMessage,
                IsVideoMessage = source.IsVideoMessage,
                IsAudioMessage = source.IsAudioMessage,
                IsReceived = source.IsReceived,
                Messages = messages
            };
        }
    }
}
