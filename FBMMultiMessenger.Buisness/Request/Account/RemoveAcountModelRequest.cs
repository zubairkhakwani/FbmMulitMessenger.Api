using FBMMultiMessenger.Contracts.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class RemoveAcountModelRequest : IRequest<BaseResponse<ToggleAcountStatusModelResponse>>
    {
        public int AccountId { get; set; }
    }

    public class ToggleAcountStatusModelResponse { }

}
