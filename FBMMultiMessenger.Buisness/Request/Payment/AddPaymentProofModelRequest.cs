using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FBMMultiMessenger.Buisness.Request.Payment
{
    public class AddPaymentProofModelRequest : IRequest<BaseResponse<AddPaymentProofModelResponse>>
    {
        public int PricingTierId { get; set; }
        public List<IFormFile> PaymentImages { get; set; } = new List<IFormFile>();
        public decimal PurchasedPrice { get; set; }
        public BillingCylce BillingCylce { get; set; }
        public string? Note { get; set; }
    }

    public class AddPaymentProofModelResponse { }



}
