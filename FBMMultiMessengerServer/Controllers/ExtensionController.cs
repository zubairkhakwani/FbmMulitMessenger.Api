using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExtensionController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public ExtensionController(IMapper mapper, IMediator mediator)
        {
            this._mapper=mapper;
            this._mediator=mediator;
        }

        [Authorize]
        [HttpPost("notify")]
        public async Task<BaseResponse<NotifyExtensionHttpResponse>> ReceiveChatMessage([FromForm] NotifyExtensionHttpRequest httpRequest)
        {
            NotifyExtensionModelRequest request = _mapper.Map<NotifyExtensionModelRequest>(httpRequest);
            var userId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value);
            request.UserId = userId;

            BaseResponse<NotifyExtensionModelResponse> response = await _mediator.Send(request);

            BaseResponse<NotifyExtensionHttpResponse> httpResponse = _mapper.Map<BaseResponse<NotifyExtensionHttpResponse>>(response);

            return httpResponse;
        }
    }
}
