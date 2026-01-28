using FBMMultiMessenger.Contracts.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class PaymentVerification
    {
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        [ForeignKey(nameof(HandledByUser))]
        public int? HandledByUserId { get; set; }

        [ForeignKey(nameof(Subscription))]
        public int? SubscriptionId { get; set; }
        public int AccountLimit { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal ActualPrice { get; set; }
        public decimal BasePricePerMonth { get; set; }
        public decimal SavingAmount { get; set; }

        public string? SubmissionNote { get; set; } // a note that can be send by the user when submitting payment verification
        public string? ReviewNote { get; set; } // a note that can be send by the admin when rejecting/approving payment verification
        public DateTime CreatedAt { get; set; }

        public PaymentStatus Status { get; set; }
        public PaymentRejectionReason RejectionReason { get; set; }

        public BillingCylce? BillingCycle { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }

        //Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual User? HandledByUser { get; set; }
        public virtual Subscription? Subscription { get; set; }
        public virtual List<PaymentVerificationImage> PaymentVerificationImages { get; set; } = new List<PaymentVerificationImage>();
    }
}
