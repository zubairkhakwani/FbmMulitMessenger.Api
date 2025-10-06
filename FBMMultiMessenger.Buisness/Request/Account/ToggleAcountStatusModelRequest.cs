using FBMMultiMessenger.Contracts.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class ToggleAcountStatusModelRequest : IRequest<BaseResponse<ToggleAcountStatusModelResponse>>
    {
        public int UserId { get; set; } //Current User Id
        public int AccountId { get; set; }
    }

    public class ToggleAcountStatusModelResponse { }

}
