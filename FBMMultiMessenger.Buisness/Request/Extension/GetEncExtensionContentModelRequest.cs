using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Extension
{
    public class GetEncExtensionContentModelRequest : IRequest<BaseResponse<GetEncExtensionContentModelResponse>>
    {
        public bool UpdateServer { get; set; }
    }

    public class GetEncExtensionContentModelResponse
    {
        public string Css { get; set; } = string.Empty;
    }
}
