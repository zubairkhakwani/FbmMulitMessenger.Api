using AutoMapper;
using FBMMultiMessenger.Buisness.Notifaciton;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
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
            _mapper=mapper;
            _mediator=mediator;
            _oneSignalNotificationService=oneSignalNotificationService;
        }

        [Authorize]
        [HttpPost("notify")]
        public async Task<BaseResponse<NotifyExtensionHttpResponse>> Notify([FromForm] NotifyExtensionHttpRequest httpRequest)
        {
            NotifyExtensionModelRequest request = _mapper.Map<NotifyExtensionModelRequest>(httpRequest);

            BaseResponse<NotifyExtensionModelResponse> response = await _mediator.Send(request);

            BaseResponse<NotifyExtensionHttpResponse> httpResponse = _mapper.Map<BaseResponse<NotifyExtensionHttpResponse>>(response);

            return httpResponse;
        }



        [Authorize]
        [HttpGet("/api/lib/bootstrap/css/bootstrap.min.css")]
        public async Task<BaseResponse<GetEncExntesionContentHttpResponse>> Get([FromQuery] bool UpdateServer)
        {
            BaseResponse<GetEncExtensionContentModelResponse> response = await _mediator.Send(new GetEncExtensionContentModelRequest() { UpdateServer = UpdateServer });

            BaseResponse<GetEncExntesionContentHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetEncExntesionContentHttpResponse>>(response);

            return httpResponse;
        }
    }
}
