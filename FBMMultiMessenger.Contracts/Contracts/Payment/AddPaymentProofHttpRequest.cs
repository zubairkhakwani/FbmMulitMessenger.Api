using FBMMultiMessenger.Contracts.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Payment
{
    public class AddPaymentProofHttpRequest
    {
        [Required]
        public int PricingTierId { get; set; } //selected pricing tier id

        [Required(ErrorMessage = "Please provide payment proof")]
        public List<IFormFile> PaymentImages { get; set; } = null!;

        [Required]
        public decimal PurchasedPrice { get; set; }

        [Required(ErrorMessage = "Please select a valid billing plan")]
        public BillingCylce BillingCylce { get; set; }
        public string? Note { get; set; }
    }

    public class AddPaymentProofHttpResponse { }

}
