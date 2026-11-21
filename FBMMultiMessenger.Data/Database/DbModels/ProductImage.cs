using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    /// <summary>
    /// Saved product photo (max 5 per product, enforced in handlers). RelativePath is served to the extension.
    /// </summary>
    public class ProductImage
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }

        /// <summary>Relative URL under wwwroot, e.g. Images/Products/{id}/{file}.</summary>
        public required string RelativePath { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }

        public Product Product { get; set; } = null!;
    }
}
