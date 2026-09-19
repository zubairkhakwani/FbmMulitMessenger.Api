using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.DefaultMessage
{
    public class UpsertDefaultMessageHttpRequest
    {
        [Required(ErrorMessage = "Please enter a default message")]
        public string Message { get; set; } = null!;

        [Required(ErrorMessage = "Please select atleast one account ")]
        public List<int> SelectedAccounts { get; set; } = new List<int>();

        /// <summary>
        /// Apply to all free accounts now and to accounts added later.
        /// </summary>
        public bool ApplyToUpcomingAccounts { get; set; }
    }

    public class UpsertDefaultMessageHttpResponse
    {

    }
}
