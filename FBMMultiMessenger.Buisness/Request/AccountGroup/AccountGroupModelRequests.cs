using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.AccountGroup
{
    public class GetMyAccountGroupsModelRequest : IRequest<BaseResponse<GetMyAccountGroupsModelResponse>>
    {
    }

    public class GetMyAccountGroupsModelResponse
    {
        public List<AccountGroupModelResponse> Groups { get; set; } = new();
    }

    public class AccountGroupModelResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public List<int> AccountIds { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpsertAccountGroupModelRequest : IRequest<BaseResponse<AccountGroupModelResponse>>
    {
        public int? GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<int> AccountIds { get; set; } = new();
    }

    public class DeleteAccountGroupModelRequest : IRequest<BaseResponse<object>>
    {
        public int GroupId { get; set; }
    }
}
