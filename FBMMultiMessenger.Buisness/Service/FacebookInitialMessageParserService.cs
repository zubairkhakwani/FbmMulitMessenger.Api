using System.Text.Json;

namespace FBMMultiMessenger.Buisness.Service
{
    public class ParsedMessage
    {
        public string MessageId { get; set; }
        public string Text { get; set; }
        public long Timestamp { get; set; }
        public string SenderId { get; set; }
        public bool IsReceived { get; set; }
        public List<string> Attachments { get; set; } = new();
        public string Type { get; set; } // text, image, video, sticker, attachment, file
        public string FbMessageReplyId { get; set; }
    }

    public class ParsedParticipant
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ProfileImage { get; set; }
        public bool IsCreator { get; set; }
    }

    public class ParsedChat
    {
        public string FbChatId { get; set; }
        public string OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string OtherUserProfilePicture { get; set; }
        public string ListingTitle { get; set; }
        public string ListingImage { get; set; }
        public List<ParsedMessage> Messages { get; set; } = new();
        public List<ParsedParticipant> Participants { get; set; } = new();
    }

    public class MessengerPayloadParser
    {
        public List<ParsedChat> ParsePayload(string payloadString, string currentUserId)
        {
            try
            {
                using var doc = JsonDocument.Parse(payloadString);
                var chats = new Dictionary<string, ParsedChat>();
                var contacts = new Dictionary<string, ParsedParticipant>();

                if (!doc.RootElement.TryGetProperty("step", out var steps))
                    return new List<ParsedChat>();

                // mirrors: for (const step of steps) this.processStep(...)
                foreach (var step in steps.EnumerateArray())
                    ProcessStep(step, chats, contacts, currentUserId);

                return FormatChats(chats, contacts, currentUserId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ParsePayload error: {ex.Message}");
                return new List<ParsedChat>();
            }
        }

        private void ProcessThread(List<JsonElement> p, Dictionary<string, ParsedChat> chats)
        {
            if (p.Count < 8) return;

            // mirrors: this.extractValue(params[7])
            var threadId = ExtractValue(p[7]);
            if (threadId == null) return;

            // mirrors: if (typeof params[3] !== 'string') return
            if (p[3].ValueKind != JsonValueKind.String) return;

            if (!chats.ContainsKey(threadId))
                chats[threadId] = new ParsedChat { FbChatId = threadId };

            chats[threadId].ListingTitle = p[3].GetString();
            chats[threadId].ListingImage = p[4].ValueKind == JsonValueKind.String
                ? p[4].GetString() : null;
        }

        private void ProcessParticipant(List<JsonElement> p, Dictionary<string, ParsedChat> chats)
        {
            if (p.Count < 2) return;

            var threadId = ExtractValue(p[0]);
            var participantId = ExtractValue(p[1]);
            if (threadId == null || participantId == null) return;

            if (!chats.ContainsKey(threadId))
                chats[threadId] = new ParsedChat { FbChatId = threadId };

            var chat = chats[threadId];

            // mirrors: if (!chat.participants.some(p => p.id === participantId))
            if (!chat.Participants.Any(x => x.Id == participantId))
            {
                chat.Participants.Add(new ParsedParticipant
                {
                    Id = participantId,
                    // mirrors: params[13] === true
                    IsCreator = p.Count > 13 && p[13].ValueKind == JsonValueKind.True,
                });
            }
        }

        private void ProcessMessage(List<JsonElement> p, Dictionary<string, ParsedChat> chats,
        string currentUserId)
        {
            if (p.Count < 9) return;

            // mirrors: if (params[12] === true) return — system message
            if (p.Count > 12 && p[12].ValueKind == JsonValueKind.True) return;

            var threadId = ExtractValue(p[3]);
            var messageId = p[8].ValueKind == JsonValueKind.String ? p[8].GetString() : null;
            if (threadId == null || messageId == null) return;

            var tsStr = ExtractValue(p[5]);
            long.TryParse(tsStr, out var timestamp);
            var senderId = p.Count > 10 ? ExtractValue(p[10]) : null;

            // mirrors: params[23] replyId
            string replyId = null;
            if (p.Count > 23 && p[23].ValueKind == JsonValueKind.String)
                replyId = p[23].GetString();

            if (!chats.ContainsKey(threadId))
                chats[threadId] = new ParsedChat { FbChatId = threadId };

            var chat = chats[threadId];

            // mirrors: if (chat.messages.some(m => m.messageId === messageId)) return
            if (chat.Messages.Any(m => m.MessageId == messageId)) return;

            var text = p[0].ValueKind == JsonValueKind.String ? p[0].GetString() : null;

            chat.Messages.Add(new ParsedMessage
            {
                MessageId = messageId,
                Text = text ?? "",
                Timestamp = timestamp,
                SenderId = senderId,
                IsReceived = senderId != currentUserId,
                Type = text != null ? "text" : "attachment",
                FbMessageReplyId = replyId,
            });
        }

        private void ProcessXmaAttachment(List<JsonElement> p, Dictionary<string, ParsedChat> chats)
        {
            if (p.Count < 31) return;

            // mirrors: params[25] threadId, params[30] messageId
            var threadId = ExtractValue(p[25]);
            var messageId = ExtractValue(p[30]);
            if (threadId == null || messageId == null) return;

            // mirrors: chat.messages = chat.messages.filter(m => m.messageId !== messageId)
            if (chats.TryGetValue(threadId, out var chat))
                chat.Messages.RemoveAll(m => m.MessageId == messageId);
        }

        private void ProcessStickerAttachment(List<JsonElement> p, Dictionary<string, ParsedChat> chats)
        {
            if (p.Count < 19) return;

            var threadId = ExtractValue(p[14]);
            var messageId = p[18].ValueKind == JsonValueKind.String ? p[18].GetString() : null;
            if (threadId == null || messageId == null) return;

            if (!chats.TryGetValue(threadId, out var chat)) return;
            var message = chat.Messages.FirstOrDefault(m => m.MessageId == messageId);
            if (message == null) return;

            // mirrors: if (typeof url !== 'string') url = urlStp
            var url = p[0].ValueKind == JsonValueKind.String
                ? p[0].GetString()
                : (p.Count > 4 && p[4].ValueKind == JsonValueKind.String
                    ? p[4].GetString() : null);

            if (url == null) return;

            message.Attachments.Add(url);
            if (string.IsNullOrEmpty(message.Text))
                message.Type = "sticker";
        }

        private void ProcessAttachment(List<JsonElement> p, Dictionary<string, ParsedChat> chats)
        {
            if (p.Count < 33) return;

            var attachmentId = p[0].ValueKind == JsonValueKind.String ? p[0].GetString() : null;
            var url = p[3].ValueKind == JsonValueKind.String ? p[3].GetString() : null;
            var threadId = ExtractValue(p[27]);
            var messageId = p[32].ValueKind == JsonValueKind.String ? p[32].GetString() : null;

            if (threadId == null || messageId == null || url == null) return;
            if (!chats.TryGetValue(threadId, out var chat)) return;

            var message = chat.Messages.FirstOrDefault(m => m.MessageId == messageId);
            if (message == null) return;

            message.Attachments.Add(url);

            if (string.IsNullOrEmpty(message.Text))
            {
                // mirrors: attachmentId.includes("image/video")
                message.Type = attachmentId?.Contains("image") == true ? "image"
                    : attachmentId?.Contains("video") == true ? "video"
                    : "file";
            }
        }

        private void ProcessContact(List<JsonElement> p,
        Dictionary<string, ParsedParticipant> contacts)
        {
            if (p.Count < 4) return;

            var contactId = ExtractValue(p[0]);
            if (contactId == null) return;

            contacts[contactId] = new ParsedParticipant
            {
                Id = contactId,
                ProfileImage = p[2].ValueKind == JsonValueKind.String ? p[2].GetString() : null,
                Name = p[3].ValueKind == JsonValueKind.String ? p[3].GetString() : null,
            };
        }

        private List<ParsedChat> FormatChats(Dictionary<string, ParsedChat> chats,
        Dictionary<string, ParsedParticipant> contacts, string currentUserId)
        {
            foreach (var chat in chats.Values)
            {
                // mirrors: chat.messages.sort((a, b) => a.timestamp - b.timestamp)
                chat.Messages = chat.Messages.OrderBy(m => m.Timestamp).ToList();

                // mirrors: enrich participants with contact info
                foreach (var p in chat.Participants)
                {
                    if (contacts.TryGetValue(p.Id, out var contact))
                    {
                        p.Name = contact.Name;
                        p.ProfileImage = contact.ProfileImage;
                    }
                }

                // mirrors: otherParticipantId = participants.find(p => p.id !== currentUserId)
                var otherParticipantId = chat.Participants
                    .FirstOrDefault(p => p.Id != currentUserId)?.Id
                    // mirrors: fallback from messages if participant not found
                    ?? chat.Messages.FirstOrDefault(m => m.SenderId != currentUserId)?.SenderId;

                contacts.TryGetValue(otherParticipantId ?? "", out var otherContact);

                chat.OtherUserId = otherParticipantId;
                chat.OtherUserName = otherContact?.Name;
                chat.OtherUserProfilePicture = otherContact?.ProfileImage;
            }

            // mirrors: sort by most recent message timestamp descending
            return chats.Values
                .OrderByDescending(c => c.Messages.LastOrDefault()?.Timestamp ?? 0)
                .ToList();
        }

        // mirrors: extractValue — handles both string and [19, "value"] array format
        private string ExtractValue(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString();

            if (element.ValueKind == JsonValueKind.Array &&
                element.GetArrayLength() >= 2)
            {
                var second = element[1];
                if (second.ValueKind == JsonValueKind.String) return second.GetString();
                if (second.ValueKind == JsonValueKind.Number) return second.GetRawText();
            }

            return null;
        }
        // end MessengerPayloadParser


        private void ProcessStep(JsonElement data, Dictionary<string, ParsedChat> chats,
            Dictionary<string, ParsedParticipant> contacts, string currentUserId)
        {
            if (data.ValueKind != JsonValueKind.Array) return;

            foreach (var item in data.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Array) continue;

                var arr = item.EnumerateArray().ToList();

                // mirrors: item[0] === 5 && typeof item[1] === 'string'
                if (arr.Count >= 2 &&
                    arr[0].ValueKind == JsonValueKind.Number && arr[0].GetInt32() == 5 &&
                    arr[1].ValueKind == JsonValueKind.String)
                {
                    var opName = arr[1].GetString();
                    var params_ = arr.Skip(2).ToList();

                    switch (opName)
                    {
                        case "deleteThenInsertThread":
                            ProcessThread(params_, chats); break;
                        case "addParticipantIdToGroupThread":
                            ProcessParticipant(params_, chats); break;
                        case "upsertMessage":
                            ProcessMessage(params_, chats, currentUserId); break;
                        case "insertBlobAttachment":
                            ProcessAttachment(params_, chats); break;
                        case "insertStickerAttachment":
                            ProcessStickerAttachment(params_, chats); break;
                        case "insertXmaAttachment":
                            ProcessXmaAttachment(params_, chats); break;
                        case "verifyContactRowExists":
                            ProcessContact(params_, contacts); break;
                    }
                }

                // mirrors: this.processStep(item, ...) — recurse
                ProcessStep(item, chats, contacts, currentUserId);
            }
        }
    }
}
