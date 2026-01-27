using FBMMultiMessenger.Contracts.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Payment
{
    public class AddPaymentProofHttpRequest
    {
        [Required(ErrorMessage = "Please provide payment proof")]
        public List<IFormFile> PaymentImages { get; set; } = null!;

        [Required]
        [Range(1, 10000, ErrorMessage = "Accounts purchased must be between 1 and 10000.")]
        public int AccountsPurchased { get; set; }

        [Required]
        public decimal PurchasedPrice { get; set; }

        [Required(ErrorMessage = "Please select a valid billing plan")]
        public BillingCylce BillingCylce { get; set; }
        public string? Note { get; set; }
    }

    public class AddPaymentProofHttpResponse { }

}
