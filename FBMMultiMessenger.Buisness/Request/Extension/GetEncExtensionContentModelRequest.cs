using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Extension
{
    public class GetEncExtensionContentModelRequest : IRequest<BaseResponse<GetEncExtensionContentModelResponse>>
    {
    }

    public class GetEncExtensionContentModelResponse
    {
        public string Css { get; set; } = string.Empty;
    }
}
