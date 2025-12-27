using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class GetAllMyAccountsChatsModelRequest : IRequest<BaseResponse<GetAllMyAccountsChatsModelResponse>>
    {
    }

    public class GetAllMyAccountsChatsModelResponse
    {
        public List<GetMyChatsModelResponse> Chats { get; set; } = new List<GetMyChatsModelResponse>();
    }

    public class GetMyChatsModelResponse
    {
        public int Id { get; set; }
        public string FbChatId { get; set; } = null!;
        public string? FbListingTitle { get; set; }
        public string? FbListingImage { get; set; }
        public string? UserProfileImage { get; set; }
        public string? FbListingLocation { get; set; }
        public decimal? FbListingPrice { get; set; }
        public string? MessagePreview { get; set; }
        public string? SenderName { get; set; } // You or Hadia.
        public bool IsRead { get; set; }
        public int UnReadCount { get; set; }
        public bool IsAccountConnected { get; set; }
        public DateTime StartedAt { get; set; }

        public GetMyChatAccountModelResponse? Account { get; set; } = new GetMyChatAccountModelResponse();

        public List<GetMyChatMessagesModelResonse> ChatMessages { get; set; } = new List<GetMyChatMessagesModelResonse>();

    }

    public class GetMyChatAccountModelResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class GetMyChatMessagesModelResonse
    {

    }
}
