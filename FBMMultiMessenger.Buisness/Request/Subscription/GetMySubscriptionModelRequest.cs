using FBMMultiMessenger.Contracts.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Subscription
{
    public class GetMySubscriptionModelRequest : IRequest<BaseResponse<GetMySubscriptionModelResponse>>
    {
        public int UserId { get; set; } //Currently logged in userid.
    }

    public class GetMySubscriptionModelResponse
    {
        public int MaxLimit { get; set; }
        public int LimitUsed { get; set; }

        public bool IsExpired { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiredAt { get; set; }
    }
}
