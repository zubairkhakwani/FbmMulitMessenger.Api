namespace FBMMultiMessenger.Buisness.Models.SignalR.App
{
    /// <summary>
    /// Sent to the app when the other participant has read (seen) the user's sent messages in a chat.
    /// The app should mark all of its sent messages in <see cref="ChatId"/> with
    /// FbTimeStamp &lt;= <see cref="SeenWatermarkMs"/> as "Seen" (displaying <see cref="SeenAt"/>).
    /// </summary>
    public class ChatMessagesSeenSignalRModel
    {
        public int ChatId { get; set; }

        /// <summary>Read watermark (Facebook ms timestamp): sent messages at/before this are seen.</summary>
        public long SeenWatermarkMs { get; set; }

        /// <summary>When the other participant read up to the watermark.</summary>
        public DateTime SeenAt { get; set; }
    }
}
