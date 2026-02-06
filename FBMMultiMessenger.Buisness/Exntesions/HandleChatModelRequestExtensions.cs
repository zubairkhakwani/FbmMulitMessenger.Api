using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Chat;

namespace FBMMultiMessenger.Buisness.Exntesions
{
    public static class HandleChatModelRequestExtensions
    {
        public static MessagePreviewRequest ToMessagePreviewRequest(this HandleChatModelRequest source, string? otherUserName)
        {
            return new MessagePreviewRequest
            {
                Messages = source.Messages,
                IsReceived = source.IsReceived,
                IsImageMessage = source.IsImageMessage,
                IsVideoMessage = source.IsVideoMessage,
                IsAudioMessage = source.IsAudioMessage,
                OtherUserName = otherUserName
            };
        }
    }
}
