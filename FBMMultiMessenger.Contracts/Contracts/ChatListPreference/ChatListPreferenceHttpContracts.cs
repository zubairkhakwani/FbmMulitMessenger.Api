namespace FBMMultiMessenger.Contracts.Contracts.ChatListPreference
{
    public class GetChatListPreferenceHttpResponse
    {
        public List<int> PinnedChatIds { get; set; } = new();
        public List<int> FavoriteChatIds { get; set; } = new();
    }

    public class UpsertChatListPreferenceHttpRequest
    {
        public List<int> PinnedChatIds { get; set; } = new();
        public List<int> FavoriteChatIds { get; set; } = new();
    }
}
