namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class UpsertChatListOrganizationHttpRequest
    {
        /// <summary>Null = leave unchanged.</summary>
        public bool? IsPinned { get; set; }

        /// <summary>Null = leave unchanged.</summary>
        public bool? IsFavorite { get; set; }
    }

    public class UpsertChatListOrganizationHttpResponse
    {
        public int ChatId { get; set; }
        public bool IsPinned { get; set; }
        public bool IsFavorite { get; set; }
        public int? PinOrder { get; set; }
        public int? FavoriteOrder { get; set; }
    }
}
