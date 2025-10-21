using FBMMultiMessenger.Contracts.Response;
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
        public int UserId { get; set; }
        public required string FbChatId { get; set; }
        public required string FbAccountId { get; set; }
        public string? FbListingId { get; set; }
        public string? FbListingTitle { get; set; }
        public string? FbListingImg { get; set; }
        public string? UserProfileImg { get; set; }

        public decimal? FbListingPrice { get; set; }
        public string? FbListingLocation { get; set; }
        public required List<string> Messages { get; set; }
        public string? OfflineUniqueId { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public bool IsReceived { get; set; } //it will only be true if the user has sent the message => (true = message is sent & false = message is received)
    }

    public class HandleChatModelResponse
    {

    }
}
