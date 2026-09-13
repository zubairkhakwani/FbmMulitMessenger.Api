using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.ApiKey
{
    public class UpsertApiKeyModelRequest : IRequest<BaseResponse<UpsertApiKeyModelResponse>>
    {
        public bool IsRegenerate { get; set; }
        public int CurrentUserId { get; set; }
    }

    public class UpsertApiKeyModelResponse
    {
        public string Key { get; set; } = string.Empty;
    }
}
