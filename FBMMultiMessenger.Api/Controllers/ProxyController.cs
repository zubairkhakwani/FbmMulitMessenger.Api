using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Request.Proxy;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Proxy;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProxyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ProxyController(IMediator mediator, IMapper mapper)
        {
            this._mediator=mediator;
            this._mapper=mapper;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<PageableResponse<GetMyProxiesHttpResponse>>> GetAll([FromQuery] GetMyProxiesHttpRequest httpRequest)
        {
            BaseResponse<PageableResponse<GetMyProxiesModelResponse>> response = await _mediator.Send(_mapper.Map<GetMyProxiesModelRequest>(httpRequest));
            BaseResponse<PageableResponse<GetMyProxiesHttpResponse>> httpResponse = _mapper.Map<BaseResponse<PageableResponse<GetMyProxiesHttpResponse>>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpPost]
        public async Task<BaseResponse<UpsertProxyHttpResponse>> Add([FromBody] UpsertProxyHttpRequest httpRequest)
        {
            BaseResponse<UpsertProxyModelResponse> response = await _mediator.Send(_mapper.Map<UpsertProxyModelRequest>(httpRequest));
            BaseResponse<UpsertProxyHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpsertProxyHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpPut("{proxyId}")]
        public async Task<BaseResponse<UpsertProxyHttpResponse>> Update([FromBody] UpsertProxyHttpRequest httpRequest, [FromRoute] int proxyId)
        {
            var request = _mapper.Map<UpsertProxyModelRequest>(httpRequest);
            request.ProxyId  = proxyId;

            BaseResponse<UpsertProxyModelResponse> response = await _mediator.Send(request);
            BaseResponse<UpsertProxyHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpsertProxyHttpResponse>>(response);

            return httpResponse;
        }
    }
}
