using AutoMapper;
using FBMMultiMessenger.Buisness.Notifaciton;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocalServerController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public LocalServerController(IMapper mapper, IMediator mediator)
        {
            _mapper=mapper;
            _mediator=mediator;
        }

        [Authorize]
        [HttpPost("notify")]
        public async Task<BaseResponse<NotifyLocalServerHttpResponse>> Notify([FromForm] NotifyLocalServerHttpRequest httpRequest)
        {
            NotifyLocalServerModelRequest request = _mapper.Map<NotifyLocalServerModelRequest>(httpRequest);

            BaseResponse<NotifyLocalServerModelResponse> response = await _mediator.Send(request);

            BaseResponse<NotifyLocalServerHttpResponse> httpResponse = _mapper.Map<BaseResponse<NotifyLocalServerHttpResponse>>(response);

            return httpResponse;
        }

    }
}
