using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class GetMyAccountsModelRequest : PageableRequest, IRequest<BaseResponse<PageableResponse<GetMyAccountsModelResponse>>>
    {
    }
}

public class GetMyAccountsModelResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Cookie { get; set; }
    public bool IsActive { get; set; }
    public int TotalCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

