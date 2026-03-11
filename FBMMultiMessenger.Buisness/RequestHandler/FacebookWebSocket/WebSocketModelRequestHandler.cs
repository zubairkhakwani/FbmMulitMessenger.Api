using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Request.FacebookWebSocket;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.FacebookWebSocket
{
    public class WebSocketModelRequestHandler : IRequestHandler<WebSocketModelRequest, BaseResponse<WebSocketModelResponse>>
    {
        private readonly IMediator mediator;

        public WebSocketModelRequestHandler(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<BaseResponse<WebSocketModelResponse>> Handle(WebSocketModelRequest request, CancellationToken cancellationToken)
        {
            var bytes = Convert.FromBase64String(request.Chunk);
            var text = Encoding.UTF8.GetString(bytes);

            if (text.Contains("insertMessage"))
            {
                var message = HandleInsertMessage(text, request);

                if(message != null)
                {
                    var mediatRRequest = new HandleChatModelRequest
                    {
                        FbChatId = message.FbChatId,
                        AccountId = request.AccountId,
                        FbAccountId = request.FbAccountId,
                        OtherUserId = null,
                        OtherUserName = null,
                        OtherUserProfilePicture = null,
                        FbMessageReplyId = message.FbMessageReplyId,
                        Messages = message.Messages,
                        OfflineUniqueId = message.OfflineUniqueId,
                        FbMessageId = message.FbMessageId,
                        Timestamp = message.Timestamp,
                        IsTextMessage = message.IsTextMessage,
                        IsVideoMessage = message.IsVideoMessage,
                        IsImageMessage = message.IsImageMessage,
                        IsAudioMessage = message.IsAudioMessage,
                        IsReceived = message.IsReceived,
                        IsNewChatStarted = message.IsNewChatStarted,
                    };

                    if(!string.IsNullOrWhiteSpace(message.FbOTID))
                    {
                        mediatRRequest.FbOTID = long.Parse(message.FbOTID);
                    }

                    await mediator.Send(mediatRRequest);
                }
            }
            else if (text.Contains("insertNewMessageRange"))
            {
                var chats = SyncExistingMessages(text, request.FbAccountId, request.AccountId);


                var messages = chats.Select(c => new SyncChatsModel
                {
                    FbChatId = c.FbChatId,
                    OtherUserId = c.OtherUserId,
                    OtherUserName = c.OtherUserName,
                    OtherUserProfilePicture = c.OtherUserProfilePicture,
                    ListingTitle = c.ListingTitle,
                    ListingImage = c.ListingImage,
                    Messages = c.Messages.Select(m => new SyncMessagesModel
                    {
                        MessageId = m.MessageId,
                        FbMessageReplyId = m.FbMessageReplyId,
                        Text = m.Text,
                        Timestamp = m.Timestamp,
                        IsReceived = m.IsReceived,
                        Attachments = m.Attachments,
                        IsTextMessage = m.Type == "text",
                        IsAudioMessage = m.Type == "audio",
                        IsImageMessage = m.Type == "image" || m.Type == "sticker",
                        IsVideoMessage = m.Type == "video",
                    }).ToList(),
                }).ToList();

                var mediatRRequest = new SyncInitialMessagesModelRequest()
                {
                    Chats = messages,
                    AccountId = request.AccountId,
                    FbAccountId = request.FbAccountId
                };

                await mediator.Send(mediatRRequest);
            }


            return null;
        }

        private InsertMessageResult HandleInsertMessage(string rawText, WebSocketModelRequest request)
        {
            var fbAccountId = request.FbAccountId;
            var accountId = request.AccountId;

            try
            {
                // mirrors: messageData = extractJsonPayload(messageData)
                var messageData = ExtractJsonPayload(rawText);

                using var doc = JsonDocument.Parse(messageData);
                var root = doc.RootElement;

                var sp = root.GetProperty("sp")
                    .EnumerateArray()
                    .Select(e => e.GetString())
                    .ToList();

                //removeAllParticipantsForThread => means new chat started
                //applyAdminMessageCTA => means zubair started this chat
                //updateAttachmentCtaAtIndexIgnoringAuthority => means message option when first message receive on any listing, like "Is this available?", Sorry, its not available, etc.
                //moveThreadToInboxAndUpdateParent => means user send a message.
                //syncBumpThreadDataToClient => Profile to profile encrypted data. in this case insertMessage does not come in sp array. so it will not reach this point.

                //This will determine whether we received a message or we sent a message.
                bool isSent = sp.Contains("moveThreadToInboxAndUpdateParent");

                //if this bit is coming it means that this is a legit message and not a system message
                //for example  "Zubair started the chat" , "Zubair named this group" so we don't need these messages
                //ONLY valid message including first initial message: "Is this available?"
                bool isLegitUserMessage = sp.Contains("updateParticipantLastMessageSendTimestamp");

                //When new chat will start we will get this sp "removeAllParticipantsForThread"
                //and for the next messages this sp will not be coming in the payload
                //so check for the first time if value is false and if it true we never set back to false as this will break the context.
                //we will only make this bit to false once we have send the default message.
                bool isNewChatStarted = sp.Contains("removeAllParticipantsForThread");

                // mirrors: if (!isLegitUserMessage) return;
                if (!isLegitUserMessage)
                {
                    Console.WriteLine("Not a legit user message, skipping.");
                    return null;
                }

                string offlineUniqueId = null;
                string? fbOTID = null;
                string fbMessageReplyId = null;

                if (isSent && request.PendingMessages?.Any() == true)
                {
                    var matched = request.PendingMessages
                        .FirstOrDefault(msg =>
                            msg.Otid != null &&
                            messageData.Contains(msg.Otid.ToString())
                        );

                    if (matched != null)
                    {
                        offlineUniqueId = matched.UniqueId;
                        fbOTID = matched.Otid;
                        fbMessageReplyId = matched.FbMessageReplyId;
                    }
                }

                // Task 3 will continue from here
                // passing messageData (trimmed), not rawText
                var message = ProcessLegitMessage(messageData, fbAccountId, accountId, isSent, isNewChatStarted, offlineUniqueId, fbOTID, fbMessageReplyId);

                return message;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"HandleInsertMessage error: {ex.Message}");
                return null;
            }
        }

        private InsertMessageResult ProcessLegitMessage(string messageData, string fbAccountId, int accountId,
    bool isSent, bool isNewChatStarted, string offlineUniqueId, string? fbOTID, string fbMessageReplyId)
        {
            using var doc = JsonDocument.Parse(messageData);
            var payloadStr = doc.RootElement.GetProperty("payload").GetString();
            using var payloadDoc = JsonDocument.Parse(payloadStr);

            var messageText = FindMessage(payloadDoc.RootElement);

            var otherData = ExtractAllMessageData(messageData);
            var fbMessageId = otherData?.MessageId;
            var timestamp = otherData?.Timestamp ?? 0;

            if (string.IsNullOrWhiteSpace(fbMessageReplyId))
            {
                fbMessageReplyId = otherData?.FbMessageReplyId;
            }

            if (fbOTID == null)
            {
                fbOTID = otherData?.Otid ?? ExtractOTIDFromReceivedMessage(messageData);
            }

            var fbChatId = ExtractChatId(messageData);

            // mirrors: mediaResult = extractMediaUrls(messageData, messageText)
            var mediaResult = ExtractMediaUrls(messageData, messageText);

            // mirrors: IsImageMessage, IsVideoMessage, IsAudioMessage, IsTextMessage
            bool isImageMessage = mediaResult.HasImages && !mediaResult.HasVideos;
            bool isVideoMessage = !isImageMessage && mediaResult.HasVideos;
            bool isAudioMessage = mediaResult.HasAudio;
            bool isTextMessage = !isImageMessage && !isVideoMessage && !isAudioMessage;

            // mirrors: if IsImage -> messages = images, elif IsVideo -> videos, etc
            List<string> messages;
            if (isImageMessage)
            {
                messages = mediaResult.Images;
            }
            else if (isVideoMessage)
            {
                messages = mediaResult.Videos;
            }
            else if (isAudioMessage)
            {
                messages = mediaResult.Audio;
            }
            else
            {
                messages = new List<string> { messageText };
            }

            var result = new InsertMessageResult
            {
                FbAccountId = fbAccountId,
                FbChatId = fbChatId,
                Messages = messages,
                OfflineUniqueId = offlineUniqueId,
                IsTextMessage = isTextMessage,
                IsImageMessage = isImageMessage,
                IsVideoMessage = isVideoMessage,
                IsAudioMessage = isAudioMessage,
                IsReceived = !isSent,           // mirrors: !IsSent
                FbOTID = fbOTID,
                FbMessageId = fbMessageId,
                FbMessageReplyId = fbMessageReplyId,
                Timestamp = timestamp,
                IsNewChatStarted = isNewChatStarted,
            };

            return result;

            // TODO: Task 5 — pass result to chat service
            Console.WriteLine($"Result built for ChatId: {result.FbChatId}, IsReceived: {result.IsReceived}, IsNew: {result.IsNewChatStarted}");
        }

        private string ExtractChatId(string rawPayload)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawPayload);
                var innerPayload = doc.RootElement
                    .GetProperty("payload")
                    .GetString()
                    ?.Replace("\\/", "/")
                    .Replace("\\\"", "\"");

                if (innerPayload == null) return null;

                // mirrors: contextRegex — most accurate
                var contextMatch = Regex.Match(
                    innerPayload,
                    @"checkAuthoritativeMessageExists"",\[19,""(\d+)""\]"
                );
                if (contextMatch.Success)
                    return contextMatch.Groups[1].Value;

                // mirrors: genericRegex fallback frequency map
                var matches = Regex.Matches(innerPayload, @"\[19,""(\d+)""\]");
                var freqMap = new Dictionary<string, int>();

                foreach (Match m in matches)
                {
                    var id = m.Groups[1].Value;
                    freqMap[id] = freqMap.GetValueOrDefault(id) + 1;
                }

                // mirrors: score = id.length * frequency
                return freqMap
                    .OrderByDescending(kv => kv.Key.Length * kv.Value)
                    .Select(kv => kv.Key)
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        private string? ExtractOTIDFromReceivedMessage(string messageData)
        {
            try
            {
                using var doc = JsonDocument.Parse(messageData);
                var payloadStr = doc.RootElement.GetProperty("payload").GetString();
                using var payloadDoc = JsonDocument.Parse(payloadStr);

                if (!payloadDoc.RootElement.TryGetProperty("step", out var step))
                    return null;

                return FindOTID(step);
            }
            catch { return null; }
        }

        private string? FindOTID(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array) return null;

            var arr = element.EnumerateArray().ToList();

            // mirrors: item[0] === 5 && item[1] === "checkAuthoritativeMessageExists" && item[3]
            if (arr.Count >= 4 &&
                arr[0].ValueKind == JsonValueKind.Number && arr[0].GetInt32() == 5 &&
                arr[1].ValueKind == JsonValueKind.String &&
                arr[1].GetString() == "checkAuthoritativeMessageExists" &&
                arr[3].ValueKind == JsonValueKind.String)
            {
                return arr[3].GetString();
            }

            foreach (var item in arr)
            {
                var result = FindOTID(item);
                if (result != null) return result;
            }

            return null;
        }

        private class MessageExtractionResult
        {
            public string MessageId { get; set; }
            public long Timestamp { get; set; }
            public string ThreadId { get; set; }
            public string MessageText { get; set; }
            public string FbMessageReplyId { get; set; }
            public string? Otid { get; set; }
        }

        private MessageExtractionResult ExtractAllMessageData(string messageData)
        {
            try
            {
                using var doc = JsonDocument.Parse(messageData);
                var payloadStr = doc.RootElement.GetProperty("payload").GetString();
                using var payloadDoc = JsonDocument.Parse(payloadStr);

                var result = new MessageExtractionResult();

                if (!payloadDoc.RootElement.TryGetProperty("step", out var step))
                    return result;

                ExtractDataRecursive(step, result);
                return result;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ExtractAllMessageData error: {ex.Message}");
                return null;
            }
        }

        private void ExtractDataRecursive(JsonElement element, MessageExtractionResult result)
        {
            if (element.ValueKind != JsonValueKind.Array) return;

            var arr = element.EnumerateArray().ToList();

            if (arr.Count >= 2 &&
                arr[0].ValueKind == JsonValueKind.Number && arr[0].GetInt32() == 5 &&
                arr[1].ValueKind == JsonValueKind.String)
            {
                var opName = arr[1].GetString();

                // mirrors: item[1] === "checkAuthoritativeMessageExists" && item[3]
                if (opName == "checkAuthoritativeMessageExists" && arr.Count > 3 &&
                    arr[3].ValueKind == JsonValueKind.String)
                {
                    result.Otid = arr[3].GetString();
                }

                // mirrors: item[1] === "insertMessage"
                if (opName == "insertMessage")
                {
                    // item[2] = message text
                    if (arr.Count > 2 && arr[2].ValueKind == JsonValueKind.String)
                        result.MessageText = arr[2].GetString();

                    // item[5] = [19, threadId]
                    if (arr.Count > 5 && arr[5].ValueKind == JsonValueKind.Array)
                        result.ThreadId = ExtractArrayValue(arr[5]);

                    // item[7] = [19, timestamp]
                    if (arr.Count > 7 && arr[7].ValueKind == JsonValueKind.Array)
                    {
                        var tsStr = ExtractArrayValue(arr[7]);
                        if (long.TryParse(tsStr, out var ts)) result.Timestamp = ts;
                    }

                    // item[10] = messageId starting with "mid.$"
                    if (arr.Count > 10 && arr[10].ValueKind == JsonValueKind.String)
                    {
                        var mid = arr[10].GetString();
                        if (mid?.StartsWith("mid.$") == true) result.MessageId = mid;
                    }

                    // item[25] = replyId starting with "mid.$"
                    if (arr.Count > 25 && arr[25].ValueKind == JsonValueKind.String)
                    {
                        var replyId = arr[25].GetString();
                        if (replyId?.StartsWith("mid.$") == true) result.FbMessageReplyId = replyId;
                    }
                }
            }

            // recurse into all children
            foreach (var item in arr)
                ExtractDataRecursive(item, result);
        }

        // mirrors: extractValue — handles [19, "value"] array format
        private string ExtractArrayValue(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() < 2)
                return null;

            var second = element[1];
            if (second.ValueKind == JsonValueKind.String) return second.GetString();
            if (second.ValueKind == JsonValueKind.Number) return second.GetRawText();
            return null;
        }

        private string FindMessage(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var arr = element.EnumerateArray().ToList();

                // mirrors: item[0] === 5 && item[1] === "insertMessage" && item[2]
                if (arr.Count >= 3 &&
                    arr[0].ValueKind == JsonValueKind.Number && arr[0].GetInt32() == 5 &&
                    arr[1].ValueKind == JsonValueKind.String && arr[1].GetString() == "insertMessage" &&
                    arr[2].ValueKind == JsonValueKind.String)
                {
                    return arr[2].GetString();
                }

                // recurse
                foreach (var item in arr)
                {
                    var result = FindMessage(item);
                    if (result != null) return result;
                }
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    var result = FindMessage(prop.Value);
                    if (result != null) return result;
                }
            }

            return null;
        }

        private class MediaResult
        {
            public bool HasImages { get; set; }
            public List<string> Images { get; set; } = new();
            public bool HasVideos { get; set; }
            public List<string> Videos { get; set; } = new();
            public bool HasAudio { get; set; }
            public List<string> Audio { get; set; } = new();
        }

        private MediaResult ExtractMediaUrls(string rawPayload, string messageText)
        {
            // mirrors: if (typeof messageText === 'string' && messageText.trim()) return empty
            if (!string.IsNullOrWhiteSpace(messageText))
                return new MediaResult();

            using var doc = JsonDocument.Parse(rawPayload);
            var innerPayload = doc.RootElement
                .GetProperty("payload")
                .GetString()
                ?.Replace("\\/", "/")
                .Replace("\\\"", "\"") ?? "";

            // mirrors: image regex
            var imageUrls = Regex.Matches(
                innerPayload,
                @"https://scontent[^"" ]+\.(?:png|jpg|jpeg|webp|gif)[^"" ]*",
                RegexOptions.IgnoreCase
            ).Select(m => m.Value).Distinct().ToList();

            // mirrors: if (imageUrls.length > 1) filter out stp=
            if (imageUrls.Count > 1)
                imageUrls = imageUrls.Where(u => !u.Contains("stp=")).ToList();

            // mirrors: video regex + dl=1 filter
            var videoUrls = Regex.Matches(
                innerPayload,
                @"https://scontent[^"" ]+\.(?:mp4|mov|avi|mkv|webm)[^"" ]*",
                RegexOptions.IgnoreCase
            ).Select(m => m.Value)
             .Where(u => u.Contains("dl=1"))
             .Distinct().ToList();

            // mirrors: facebook audio (cdn.fbsbx.com)
            var fbAudioUrls = Regex.Matches(
                innerPayload,
                @"https://cdn\.fbsbx\.com[^"" ]+\.mp4[^"" ]*",
                RegexOptions.IgnoreCase
            ).Select(m => m.Value).ToList();

            // mirrors: general audio formats
            var generalAudioUrls = Regex.Matches(
                innerPayload,
                @"https://[^"" ]+\.(?:mp3|wav|m4a|aac|ogg|flac)[^"" ]*",
                RegexOptions.IgnoreCase
            ).Select(m => m.Value).ToList();

            // mirrors: allAudioUrls = [...audioUrls, ...generalAudioUrls]
            var allAudio = fbAudioUrls.Concat(generalAudioUrls).Distinct().ToList();

            return new MediaResult
            {
                HasImages = imageUrls.Any(),
                Images = imageUrls,
                HasVideos = videoUrls.Any(),
                Videos = videoUrls,
                HasAudio = allAudio.Any(),
                Audio = allAudio,
            };
        }

        public class InsertMessageResult
        {
            public string FbAccountId { get; set; }
            public string FbChatId { get; set; }
            public List<string> Messages { get; set; } = new();
            public string OfflineUniqueId { get; set; }
            public bool IsTextMessage { get; set; }
            public bool IsImageMessage { get; set; }
            public bool IsVideoMessage { get; set; }
            public bool IsAudioMessage { get; set; }
            public bool IsReceived { get; set; }
            public string? FbOTID { get; set; }
            public string FbMessageId { get; set; }
            public string FbMessageReplyId { get; set; }
            public long Timestamp { get; set; }
            public bool IsNewChatStarted { get; set; }
        }

        private List<ParsedChat> SyncExistingMessages(string rawText, string fbAccountId, int accountId)
        {
            try
            {
                // mirrors: messageData = extractJsonPayload(messageData)
                var messageData = ExtractJsonPayload(rawText);

                using var doc = JsonDocument.Parse(messageData);

                // mirrors: data.payload passed to parsePayload
                var payloadStr = doc.RootElement.GetProperty("payload").GetString();

                var parser = new MessengerPayloadParser();
                var chats = parser.ParsePayload(payloadStr, fbAccountId);

                // TODO: pass chats to your service
                Console.WriteLine($"Synced {chats.Count} chats for fbAccountId: {fbAccountId}");

                return chats;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"SyncExistingMessages error: {ex.Message}");
                return new();
            }
        }

        private string ExtractJsonPayload(string rawMessage)
        {
            var marker = "{\"request_id\":";
            var index = rawMessage.IndexOf(marker, StringComparison.Ordinal);
            if (index == -1)
                throw new Exception("No JSON payload found in message");
            return rawMessage.Substring(index);
        }
    }
}
