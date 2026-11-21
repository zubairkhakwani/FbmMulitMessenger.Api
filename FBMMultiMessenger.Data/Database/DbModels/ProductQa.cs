using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    /// <summary>
    /// Q&A entry owned by a product. QuestionVariants is a JSON array of strings.
    /// </summary>
    public class ProductQa
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }

        public string? IntentKey { get; set; }

        public required string QuestionVariants { get; set; }

        public required string Answer { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Product Product { get; set; } = null!;
    }
}
