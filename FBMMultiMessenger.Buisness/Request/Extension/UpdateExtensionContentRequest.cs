using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Extension
{
    public class UpdateExtensionContentRequest : IRequest<BaseResponse<GetEncExtensionContentModelResponse>>
    {
    }
}
