using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Auth
{
    public class ResetPasswordHttpRequest
    {
        [Required(ErrorMessage = "Please enter new password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
         ErrorMessage = "Password must match all the requirements")]
        public string NewPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter confirm password")]
        [Compare(nameof(NewPassword), ErrorMessage = "Password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter verification code")]
        public string Otp { get; set; } = string.Empty;
    }
}
