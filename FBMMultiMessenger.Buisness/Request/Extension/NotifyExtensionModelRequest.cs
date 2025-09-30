using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Extension
{
    public class NotifyExtensionModelRequest : IRequest<BaseResponse<NotifyExtensionModelResponse>>
    {
        public int UserId { get; set; } // Current user id
        public string FbChatId { get; set; } = null!;
        public string Message { get; set; } = null!;

        public List<IFormFile>? Files { get; set; }
    }

    public class NotifyExtensionModelResponse
    {
    }
}
