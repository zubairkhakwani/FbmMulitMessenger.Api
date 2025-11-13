using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Auth
{
    public class ForgotPasswordHttpRequest
    {
        [Required(ErrorMessage = "Please enter your registered email")]
        [EmailAddress(ErrorMessage = "Please enter valid email address")]
        public string Email { get; set; } = string.Empty;
    }
}
