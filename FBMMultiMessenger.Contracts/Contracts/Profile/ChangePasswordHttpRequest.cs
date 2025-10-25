using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Profile
{
    public class ChangePasswordHttpRequest
    {
        [Required(ErrorMessage = "Please enter your current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter new password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
         ErrorMessage = "Password must match all the requirements")]
        public string NewPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please enter new password")]
        [Compare(nameof(NewPassword), ErrorMessage = "Password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
