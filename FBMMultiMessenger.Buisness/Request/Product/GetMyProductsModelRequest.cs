using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Product
{
    public class GetMyProductsModelRequest : IRequest<BaseResponse<GetMyProductsModelResponse>>
    {
    }

    public class GetMyProductsModelResponse
    {
        public List<ProductModelItem> Items { get; set; } = new();
    }

    public class ProductModelItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public List<string> ListingTitles { get; set; } = new();
        public List<int> AccountIds { get; set; } = new();
        public List<ProductAccountModelItem> Accounts { get; set; } = new();
        public List<ProductQaModelItem> Qas { get; set; } = new();
        public List<ProductImageModelItem> Images { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ProductAccountModelItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProductQaModelItem
    {
        public int Id { get; set; }
        public string? IntentKey { get; set; }
        public List<string> Questions { get; set; } = new();
        public string Answer { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ProductImageModelItem
    {
        public int Id { get; set; }
        public string RelativePath { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        /// <summary>On-disk size in bytes (0 if missing).</summary>
        public long SizeBytes { get; set; }
    }
}
