using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class UpdateAccountStatusFromExtensionRequest : IRequest<BaseResponse<UpdateAccountStatusFromExtensionResponse>>
    {
        public int AccountId { get; set; }
        public AccountConnectionStatus AccountConnectionStatus { get; set; }
        public AccountAuthStatus AccountAuthStatus { get; set; }
        public AccountReason Reason { get; set; }
        public bool IsLoggedIn { get; set; }
    }

    public class UpdateAccountStatusFromExtensionResponse
    {

    }
}
