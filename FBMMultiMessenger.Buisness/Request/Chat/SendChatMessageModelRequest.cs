using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class SendChatMessageModelRequest : IRequest<BaseResponse<SendChatMessageModelResponse>>
    {
        public int UserId { get; set; }
        public required string FbChatId { get; set; }
        public required string FbAccountId { get; set; }
        public required string FbListingId { get; set; }
        public required string FbListingTitle { get; set; }
        public required decimal FbListingPrice { get; set; }
        public required string FbListingLocation { get; set; }
        public required List<string> Messages { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
    }
    public class SendChatMessageModelResponse
    {
        public int ChatId { get; set; }
        public string FbChatId { get; set; } = null!;

        public string Message { get; set; } = null!;
    }
}
