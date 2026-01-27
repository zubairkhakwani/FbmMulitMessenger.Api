using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FBMMultiMessenger.Buisness.Request.Payment
{
    public class AddPaymentProofModelRequest : IRequest<BaseResponse<AddPaymentProofModelResponse>>
    {
        public List<IFormFile> PaymentImages { get; set; } = new List<IFormFile>();
        public int AccountsPurchased { get; set; }
        public decimal PurchasedPrice { get; set; }
        public BillingCylce BillingCylce { get; set; }
        public string? Note { get; set; }
    }

    public class AddPaymentProofModelResponse { }



}
