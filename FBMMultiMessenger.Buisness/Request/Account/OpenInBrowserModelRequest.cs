using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class OpenInBrowserModelRequest : IRequest<BaseResponse<object>>
    {
        public int AccountId { get; set; }
    }

}
