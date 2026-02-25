using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class GetAccountsStatusModelRequest : IRequest<BaseResponse<GetAccountsStatusModelResponse>>
    {
    }

    public class GetAccountsStatusModelResponse
    {
        public List<AccountStatusResponse> Statuses { get; set; }
    }

    public class AccountStatusResponse
    {
        public int Id { get; set; }
        public bool IsConnected { get; set; }
    }
}
