using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.DefaultMessage
{
    public class UpsertDefaultMessageModelRequest : IRequest<BaseResponse<UpsertDefaultMessageModelResponse>>
    {
        public int? Id { get; set; }
        public string Message { get; set; } = null!;

        public int CurrentUserId { get; set; }
        public List<int> SelectedAccounts { get; set; } = new List<int>();
        public bool ApplyToUpcomingAccounts { get; set; }
    }

    public class UpsertDefaultMessageModelResponse { }

}
