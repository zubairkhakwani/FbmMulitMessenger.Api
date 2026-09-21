using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class RegisterFacebookAccountFromExtensionRequest : IRequest<BaseResponse<RegisterFacebookAccountFromExtensionResponse>>
    {
        [Required]
        public string FbAccountId { get; set; }
    }

    public class RegisterFacebookAccountFromExtensionResponse
    {
        public int AccountId { get; set; }

        /// <summary>True when the user cannot add more accounts on their current plan.</summary>
        public bool IsLimitExceeded { get; set; }
    }

}
