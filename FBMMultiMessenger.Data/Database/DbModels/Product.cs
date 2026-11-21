using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    /// <summary>
    /// Seller product for auto-reply. Owned by a user; linked to one or more FB accounts
    /// via <see cref="AccountProduct"/> (accounts may have many products).
    /// </summary>
    public class Product
    {
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }

        public string? Category { get; set; }

        /// <summary>
        /// JSON array of Marketplace listing titles used to match incoming chat FbListingTitle.
        /// </summary>
        public string ListingTitles { get; set; } = "[]";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public List<ProductQa> Qas { get; set; } = new();
        public List<ProductImage> Images { get; set; } = new();
        public List<AccountProduct> AccountProducts { get; set; } = new();
    }
}
