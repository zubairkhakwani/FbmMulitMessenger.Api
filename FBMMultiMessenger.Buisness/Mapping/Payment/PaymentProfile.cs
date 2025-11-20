using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Payment;
using FBMMultiMessenger.Contracts.Contracts.Payment;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.Payment
{
    internal class PaymentProfile : Profile
    {
        public PaymentProfile()
        {
            CreateMap<AddPaymentProofHttpRequest, AddPaymentProofModelRequest>();
            CreateMap<AddPaymentProofModelResponse, AddPaymentProofHttpResponse>();
            CreateMap<BaseResponse<AddPaymentProofModelResponse>, BaseResponse<AddPaymentProofHttpResponse>>();
        }
    }
}
