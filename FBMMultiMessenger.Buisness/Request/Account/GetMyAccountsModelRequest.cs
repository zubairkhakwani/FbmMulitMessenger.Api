using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class GetMyAccountsModelRequest : PageableRequest, IRequest<BaseResponse<UserAccountsOverviewModelResponse>>
    {
    }
}

public class UserAccountsOverviewModelResponse
{
    public PageableResponse<UserAccountsModelResponse> UserAccounts { get; set; } = null!;
    public int ConnectedAccounts { get; set; }
    public int NotConnectedAccounts { get; set; }
}



public class UserAccountsModelResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Cookie { get; set; }
    public string? DefaultMessage { get; set; }
    public string AuthStatus { get; set; } = string.Empty;
    public string ConnectionStatus { get; set; } = string.Empty;
    public AccountProxyModelResponse? Proxy { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AccountProxyModelResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ip_Port { get; set; } = string.Empty;
}


