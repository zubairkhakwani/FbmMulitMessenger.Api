using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
