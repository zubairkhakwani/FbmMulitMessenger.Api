using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class AllocateAccountsModelRequest : IRequest<BaseResponse<List<AllocateAccountsModelResponse>>>
    {
        public int Count { get; set; }

        public string LocalServerId { get; set; } = null!;
    }

    public class AllocateAccountsModelResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public string? DefaultMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
