using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Extension
{
    public class GetExtensionVersionRequest : IRequest<BaseResponse<GetExtensionVersionModelResponse>>
    {
    }

    public class GetExtensionVersionModelResponse
    {
        public string Version { get; set; } = string.Empty;
    }
}
