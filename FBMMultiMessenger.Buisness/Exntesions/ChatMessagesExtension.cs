using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Data.Database.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                try
                {
                    messages = JsonSerializer.Deserialize<List<string>>(source.Message) ?? new List<string> { source.Message };
                }
                catch (JsonException)
                {
                    // Not JSON, treat as plain string, because we are saving plain text as well and JSON is for only multiple messages 
                    messages = new List<string> { source.Message };
                }
            }
            return new MessagePreviewRequest()
            {
                FbListingTitle = fbListingTitle,
                IsImageMessage = source.IsImageMessage,
                IsVideoMessage = source.IsVideoMessage,
                IsAudioMessage = source.IsAudioMessage,
                IsReceived = source.IsReceived,
                Messages = messages
            };
        }
    }
}
