using AutoMapper;
using FBMMultiMessenger.Buisness.Notifaciton;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExtensionController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly OneSignalService _oneSignalNotificationService;

        public ExtensionController(IMapper mapper, IMediator mediator, OneSignalService oneSignalNotificationService)
        {
            this._mapper=mapper;
            this._mediator=mediator;
            this._oneSignalNotificationService=oneSignalNotificationService;
        }

        [Authorize]
        [HttpPost("notify")]
        public async Task<BaseResponse<NotifyExtensionHttpResponse>> Notify([FromForm] NotifyExtensionHttpRequest httpRequest)
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
