using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.ApiKey
{
    public class GetMyApiKeyModelRequest : IRequest<BaseResponse<GetMyApiKeyModelResponse>>
    {

    }

    public class GetMyApiKeyModelResponse
    {
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
