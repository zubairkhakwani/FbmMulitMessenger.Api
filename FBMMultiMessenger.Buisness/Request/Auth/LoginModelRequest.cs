using FBMMultiMessenger.Contracts.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Auth
{
    public class LoginModelRequest : IRequest<BaseResponse<LoginModelResponse>>
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginModelResponse
    {
        public string? Token { get; set; }
        public bool IsSubscriptionExpired { get; set; }
    }
}
