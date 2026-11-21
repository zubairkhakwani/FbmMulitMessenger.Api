using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Product
{
    public class UpsertProductHttpRequest
    {
        [Required(ErrorMessage = "Please enter a product name")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Optional Marketplace listing titles used to match incoming chats.</summary>
        public List<string> ListingTitles { get; set; } = new();

        /// <summary>FB account ids linked to this product (accounts may have many products).</summary>
        public List<int> SelectedAccounts { get; set; } = new();

        [Required(ErrorMessage = "Please add at least one knowledge entry")]
        [MinLength(1, ErrorMessage = "Please add at least one knowledge entry")]
        public List<UpsertProductQaHttpItem> Qas { get; set; } = new();
    }

    public class UpsertProductQaHttpItem
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Please add at least one topic")]
        [MinLength(1, ErrorMessage = "Please add at least one topic")]
        public List<string> Questions { get; set; } = new();

        [Required(ErrorMessage = "Please enter knowledge information")]
        public string Answer { get; set; } = string.Empty;

        public string? IntentKey { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpsertProductHttpResponse
    {
        public int Id { get; set; }
    }

    public class DeleteProductHttpResponse
    {
    }
}
