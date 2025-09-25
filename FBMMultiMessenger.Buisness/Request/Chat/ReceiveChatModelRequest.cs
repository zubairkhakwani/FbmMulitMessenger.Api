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
    public class ReceiveChatModelRequest : IRequest<BaseResponse<ReceiveChatModelResponse>>
    {
        public int UserId { get; set; }
        public int AccountId { get; set; }
        public required string FbUserId { get; set; }
        public required string FbChatId { get; set; }
        public required string FbAccountId { get; set; }
        public required string FbListingId { get; set; }
        public required string FbListingTitle { get; set; }
        public required decimal FbListingPrice { get; set; }
        public required string FbListingLocation { get; set; }

        public required string Message { get; set; }

        public bool IsReceived { get; set; }
    }

    public class ReceiveChatModelResponse
    {

    }
}
