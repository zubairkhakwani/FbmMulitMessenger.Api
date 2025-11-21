using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Contracts.Contracts.Payment
{
    public class GetMyVerificationStatusHttpResponse
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public int AccountsPurchased { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal ActualPrice { get; set; }

        public string? ReviewNote { get; set; } // a note that can be send by the admin when rejecting/approving payment verification
        public string RejectonReason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
    }
}
