namespace FBMMultiMessenger.Contracts.Contracts.Product
{
    public class GetMyProductsHttpRequest
    {
    }

    public class GetMyProductsHttpResponse
    {
        public List<ProductHttpItem> Items { get; set; } = new();
    }

    public class ProductHttpItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public List<string> ListingTitles { get; set; } = new();
        public List<int> AccountIds { get; set; } = new();
        public List<ProductAccountHttpItem> Accounts { get; set; } = new();
        public List<ProductQaHttpItem> Qas { get; set; } = new();
        public List<ProductImageHttpItem> Images { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ProductAccountHttpItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProductQaHttpItem
    {
        public int Id { get; set; }
        public string? IntentKey { get; set; }
        public List<string> Questions { get; set; } = new();
        public string Answer { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ProductImageHttpItem
    {
        public int Id { get; set; }
        public string RelativePath { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        /// <summary>On-disk size in bytes (0 if missing).</summary>
        public long SizeBytes { get; set; }
    }
}
