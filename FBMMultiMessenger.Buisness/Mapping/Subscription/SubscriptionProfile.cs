using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Subscription;
using FBMMultiMessenger.Contracts.Contracts.Subscription;
using FBMMultiMessenger.Contracts.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Mapping.Subscription
{
    public class SubscriptionProfile : Profile
    {
        public SubscriptionProfile()
        {
            CreateMap<GetMySubscriptionModelResponse, GetMySubscriptionHttpResponse>();
            CreateMap<BaseResponse<GetMySubscriptionModelResponse>, BaseResponse<GetMySubscriptionHttpResponse>>();

        }
    }
}
