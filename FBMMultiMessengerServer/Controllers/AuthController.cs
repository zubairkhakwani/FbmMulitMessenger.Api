using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessengerServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public AuthController(IMediator mediator, IMapper mapper)
        {
            this._mediator=mediator;
            this._mapper=mapper;
        }

        [HttpPost("Login")]
        public async Task<BaseResponse<LoginHttpResponse>> Login([FromBody] LoginHttpRequest httpRequest)
        {
            LoginModelRequest request = _mapper.Map<LoginModelRequest>(httpRequest);
            BaseResponse<LoginModelResponse> response = await _mediator.Send(request);

            BaseResponse<LoginHttpResponse> httpResponse = _mapper.Map<BaseResponse<LoginHttpResponse>>(response);

            return httpResponse;
        }
    }
}

