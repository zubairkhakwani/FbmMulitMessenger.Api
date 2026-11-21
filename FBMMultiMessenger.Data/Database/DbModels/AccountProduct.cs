using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    /// <summary>
    /// Many-to-many link: one account can sell many products; one product can link to many accounts.
    /// </summary>
    public class AccountProduct
    {
        [ForeignKey(nameof(Account))]
        public int AccountId { get; set; }

        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }

        public DateTime CreatedAt { get; set; }

        public Account Account { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
