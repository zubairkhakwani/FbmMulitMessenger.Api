using AutoMapper;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Contracts.LocalServer;
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

        [Authorize]
        [HttpPost("register")]
        public async Task<BaseResponse<RegisterLocalServerHttpResponse>> Register([FromBody] RegisterLocalServerHttpRequest httpRequest)
        {
            RegisterLocalServerModelRequest request = _mapper.Map<RegisterLocalServerModelRequest>(httpRequest);

            BaseResponse<RegisterLocalServerModelResponse> response = await _mediator.Send(request);

            BaseResponse<RegisterLocalServerHttpResponse> httpResponse = _mapper.Map<BaseResponse<RegisterLocalServerHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpGet("{serverId}/accounts")]
        public async Task<BaseResponse<List<GetLocalServerAccountsHttpResponse>>> GetLocalServerAccounts([FromRoute] string serverId, [FromQuery] int limit)
        {
            var request = new GetLocalServerAccountsModelRequest()
            {
                LocalServerId = serverId,
                Limit = limit
            };

            BaseResponse<List<GetLocalServerAccountsModelResponse>> response = await _mediator.Send(request);
            BaseResponse<List<GetLocalServerAccountsHttpResponse>> httpResponse = _mapper.Map<BaseResponse<List<GetLocalServerAccountsHttpResponse>>>(response);

            return httpResponse;
        }


        [Authorize]
        [HttpGet("heartbeat")]
        public async Task<BaseResponse<LocalServerHeartBeatHttpResponse>> Hearbeat([FromBody] LocalServerHeartBeatHttpRequet httpRequet)
        {
            BaseResponse<LocalServerHeartbeatModelResponse> response = await _mediator.Send(_mapper.Map<LocalServerHeartbeatModelRequest>(httpRequet));
            BaseResponse<LocalServerHeartBeatHttpResponse> httpResponse = _mapper.Map<BaseResponse<LocalServerHeartBeatHttpResponse>>(response);

            return httpResponse;
        }
    }
}
