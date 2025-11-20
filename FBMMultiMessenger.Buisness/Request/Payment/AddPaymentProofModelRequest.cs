using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FBMMultiMessenger.Buisness.Request.Payment
{
    public class AddPaymentProofModelRequest : IRequest<BaseResponse<AddPaymentProofModelResponse>>
    {
        public IFormFile PaymentImg { get; set; }

        public int AccountsPurchased { get; set; }

        public decimal PurchasedPrice { get; set; }
        public string? Note { get; set; }
    }

    public class AddPaymentProofModelResponse { }



}
