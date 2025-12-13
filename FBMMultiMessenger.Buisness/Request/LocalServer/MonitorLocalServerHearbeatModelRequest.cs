using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.LocalServer
{
    public class MonitorLocalServerHearbeatModelRequest : IRequest<BaseResponse<MonitorLocalServerHearbeatModelResponse>>
    {
    }

    public class MonitorLocalServerHearbeatModelResponse
    {
    }
}
