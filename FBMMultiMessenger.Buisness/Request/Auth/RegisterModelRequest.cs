using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Auth
{
    public class RegisterModelRequest : IRequest<BaseResponse<RegisterModelResponse>>
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string ContactNumber { get; set; }
    }

    public class RegisterModelResponse
    {
        public bool HasAvailedTrial { get; set; }
        public int TrialDays { get; set; }
        public int TrialAccounts { get; set; }
    }
}
