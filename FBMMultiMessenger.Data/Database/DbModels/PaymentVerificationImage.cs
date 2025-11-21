using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class PaymentVerificationImage
    {
        public int Id { get; set; }

        [ForeignKey(nameof(PaymentVerification))]
        public int PaymentVerificationId { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }

        //Navigation Property
        public PaymentVerification PaymentVerification { get; set; } = null!;
    }
}
