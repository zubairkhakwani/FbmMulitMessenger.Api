using AutoMapper;
using FBMMultiMessenger.Buisness.Request.ApiKey;
using FBMMultiMessenger.Contracts.Contracts.ApiKey;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiKeyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ApiKeyController(IMediator mediator, IMapper mapper)
        {
            this._mediator=mediator;
            this._mapper=mapper;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<GetMyApiKeyHttpResponse>> GetMyApiKey()
        {
            BaseResponse<GetMyApiKeyModelResponse> response = await _mediator.Send(new GetMyApiKeyModelRequest());
            BaseResponse<GetMyApiKeyHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetMyApiKeyHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpPost]
        public async Task<BaseResponse<UpsertApiKeyHttpResponse>> Generate()
        {
            BaseResponse<UpsertApiKeyModelResponse> response = await _mediator.Send(new UpsertApiKeyModelRequest());
            BaseResponse<UpsertApiKeyHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpsertApiKeyHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpPut("regenerate")]
        public async Task<BaseResponse<UpsertApiKeyHttpResponse>> Regenerate()
        {
            BaseResponse<UpsertApiKeyModelResponse> response = await _mediator.Send(new UpsertApiKeyModelRequest() { IsRegenerate = true });
            BaseResponse<UpsertApiKeyHttpResponse> httpResponse = _mapper.Map<BaseResponse<UpsertApiKeyHttpResponse>>(response);

            return httpResponse;
        }
    }
}
