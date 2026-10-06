using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    /// <summary>
    /// Raised when Facebook sends a read receipt (someone read messages in a thread). Marks the user's
    /// sent messages up to the read watermark as seen and notifies the app.
    /// </summary>
    public class MarkMessagesSeenModelRequest : IRequest
    {
        public int AccountId { get; set; }

        /// <summary>The account's own Facebook user id (c_user). Used to ignore the account's own reads.</summary>
        public string FbAccountId { get; set; } = string.Empty;

        /// <summary>The Facebook thread id (FBChatId).</summary>
        public string FbChatId { get; set; } = string.Empty;

        /// <summary>Facebook user id of whoever performed the read.</summary>
        public string ReaderUserId { get; set; } = string.Empty;

        /// <summary>Read watermark (Facebook ms timestamp): sent messages at/before this are seen.</summary>
        public long WatermarkMs { get; set; }

        /// <summary>When the read happened (Facebook ms timestamp).</summary>
        public long ReadActionMs { get; set; }

        public int CurrentUserId { get; set; }
    }
}
