using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class HandleChatModelRequest : IRequest<BaseResponse<HandleChatModelResponse>>
    {
        public required string FbChatId { get; set; }
        public int AccountId { get; set; }
        public required string FbAccountId { get; set; }
        public string? FbListingId { get; set; }
        public string? FbListingTitle { get; set; }
        public string? FbListingImg { get; set; }
        public string? UserProfileImg { get; set; }

        public decimal? FbListingPrice { get; set; }
        public string? FbListingLocation { get; set; }
        public required List<string> Messages { get; set; }
        public string? OfflineUniqueId { get; set; }
        public long? FbOTID { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public bool IsReceived { get; set; }
    }

    public class HandleChatModelResponse
    {

    }
}
