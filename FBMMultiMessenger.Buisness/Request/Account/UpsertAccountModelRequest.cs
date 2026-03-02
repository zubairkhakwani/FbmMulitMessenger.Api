using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class UpsertAccountModelRequest : IRequest<BaseResponse<UpsertAccountModelResponse>>
    {
        public int? AccountId { get; set; }
        public int UserId { get; set; } //Current User Id
        public string Name { get; set; } = null!;
        public string Cookie { get; set; } = null!;
        public string? ProxyId { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class UpsertAccountModelResponse
    {
        public bool IsLimitExceeded { get; set; }
        public bool IsSubscriptionExpired { get; set; }
        public bool IsEmailVerified { get; set; } = true;
        public string EmailSendTo { get; set; } = string.Empty;

        public int TotalProcessed { get; set; }
        public int SuccessfullyValidated { get; set; }
        public List<SkippedAccountModelResponse> SkippedAccounts { get; set; } = new();
    }

    public class SkippedAccountModelResponse
    {
        public string UserName { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProxyId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
