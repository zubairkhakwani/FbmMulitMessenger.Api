namespace FBMMultiMessenger.Buisness.Models
{
    public class EmailVerificationResponse
    {
        public bool IsEmailVerified { get; set; }
        public string EmailSendTo { get; set; } = string.Empty;
    }
}
