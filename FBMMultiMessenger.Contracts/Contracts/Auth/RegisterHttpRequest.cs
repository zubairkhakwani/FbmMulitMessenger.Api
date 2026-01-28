using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Auth
{
    public class RegisterHttpRequest
    {
        [Required(ErrorMessage = "Please enter your name")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter your email")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter confirm password")]
        [Compare(nameof(Password),ErrorMessage = "Password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your contact number")]
        public string ContactNumber { get; set; } = string.Empty;
    }

    public class RegisterHttpResponse
    {

    }
}
