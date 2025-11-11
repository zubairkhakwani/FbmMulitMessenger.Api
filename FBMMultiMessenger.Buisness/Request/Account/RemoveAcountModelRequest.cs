using FBMMultiMessenger.Contracts.Shared;
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
        public List<int> AccountIds { get; set; } = new List<int>();
    }

    public class ToggleAcountStatusModelResponse { }

}
