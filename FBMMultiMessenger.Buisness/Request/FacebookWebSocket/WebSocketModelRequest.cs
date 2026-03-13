using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.FacebookWebSocket
{
    public class WebSocketModelRequest : IRequest<BaseResponse<WebSocketModelResponse>>
    {
        [JsonPropertyName("a")]
        public string FbAccountId { get; set; }

        [JsonPropertyName("b")]
        public string Chunk { get; set; }         // base64 encoded

        [JsonPropertyName("c")]
        public List<PendingMessage> PendingMessages { get; set; } = new();

        [JsonPropertyName("d")]
        public int AccountId { get; set; }
    }

    public class PendingMessage
    {
        public string UniqueId { get; set; }
        public string? Otid { get; set; }
        public string? FbMessageReplyId { get; set; }
    }

    public class WebSocketModelResponse
    {

    }

}
