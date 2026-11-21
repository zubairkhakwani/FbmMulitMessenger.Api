using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Product
{
    public class UpsertProductModelRequest : IRequest<BaseResponse<UpsertProductModelResponse>>
    {
        public int? Id { get; set; }
        public int CurrentUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> ListingTitles { get; set; } = new();
        public List<int> SelectedAccounts { get; set; } = new();
        public List<UpsertProductQaModelItem> Qas { get; set; } = new();
    }

    public class UpsertProductQaModelItem
    {
        public int? Id { get; set; }
        public List<string> Questions { get; set; } = new();
        public string Answer { get; set; } = string.Empty;
        public string? IntentKey { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpsertProductModelResponse
    {
        public int Id { get; set; }
    }
}
