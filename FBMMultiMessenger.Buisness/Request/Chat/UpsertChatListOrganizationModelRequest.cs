using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class UpsertChatListOrganizationModelRequest : IRequest<BaseResponse<UpsertChatListOrganizationModelResponse>>
    {
        public int ChatId { get; set; }

        /// <summary>Null = leave unchanged.</summary>
        public bool? IsPinned { get; set; }

        /// <summary>Null = leave unchanged.</summary>
        public bool? IsFavorite { get; set; }
    }

    public class UpsertChatListOrganizationModelResponse
    {
        public int ChatId { get; set; }
        public bool IsPinned { get; set; }
        public bool IsFavorite { get; set; }
        public int? PinOrder { get; set; }
        public int? FavoriteOrder { get; set; }
    }
}
