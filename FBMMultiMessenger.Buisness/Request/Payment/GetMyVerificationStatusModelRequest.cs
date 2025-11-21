using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Payment
{
    public class GetMyVerificationStatusModelRequest : IRequest<BaseResponse<GetMyVerificationStatusModelResponse>>
    {
    }

    public class GetMyVerificationStatusModelResponse
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public int AccountsPurchased { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal ActualPrice { get; set; }

        public string? ReviewNote { get; set; } // a note that can be send by the admin when rejecting/approving payment verification
        public string Description { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
    }
}
