using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Chat;

namespace FBMMultiMessenger.Buisness.Exntesions
{
    public static class HandleChatModelRequestExtensions
    {
        public static MessagePreviewRequest ToMessagePreviewRequest(this HandleChatModelRequest source)
        {
            return new MessagePreviewRequest
            {
                Messages = source.Messages,
                IsReceived = source.IsReceived,
                IsImageMessage = source.IsImageMessage,
                IsVideoMessage = source.IsVideoMessage,
                IsAudioMessage = source.IsAudioMessage,
                FbListingTitle = source.FbListingTitle
            };
        }
    }
}
