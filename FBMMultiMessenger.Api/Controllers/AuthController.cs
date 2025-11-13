using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public AuthController(IMediator mediator, IMapper mapper)
        {
            _mediator=mediator;
            _mapper=mapper;
        }

        [HttpPost("Login")]
        public async Task<BaseResponse<LoginHttpResponse>> Login([FromBody] LoginHttpRequest httpRequest)
        {
            LoginModelRequest request = _mapper.Map<LoginModelRequest>(httpRequest);
            BaseResponse<LoginModelResponse> response = await _mediator.Send(request);

            BaseResponse<LoginHttpResponse> httpResponse = _mapper.Map<BaseResponse<LoginHttpResponse>>(response);

            return httpResponse;
        }

        [HttpPost("Register")]
        public async Task<BaseResponse<RegisterHttpResponse>> Register([FromBody] RegisterHttpRequest httpRequest)
        {
            RegisterModelRequest request = _mapper.Map<RegisterModelRequest>(httpRequest);
            BaseResponse<RegisterModelResponse> response = await _mediator.Send(request);

            BaseResponse<RegisterHttpResponse> httpResponse = _mapper.Map<BaseResponse<RegisterHttpResponse>>(response);

            return httpResponse;
        }

        [HttpPost("forgot-password")]
        public async Task<BaseResponse<object>> ForgotPassword([FromBody] ForgotPasswordHttpRequest httpRequest)
        {
            BaseResponse<object> httpResponse = await _mediator.Send(new ForgotPasswordModelRequest() { Email = httpRequest.Email });
            return httpResponse;
        }

        [HttpPost("verify-otp")]
        public async Task<BaseResponse<object>> VerifyOtp([FromBody] string otp, [FromQuery] bool isEmailVerification = false)
        {
            BaseResponse<object> httpResponse = await _mediator.Send(new VerifyOtpModelRequest() { Otp = otp, IsEmailVerification = isEmailVerification });
            return httpResponse;
        }

        [HttpPost("resend-otp")]
        public async Task<BaseResponse<object>> ResendOtp([FromBody] string email, [FromQuery] bool isEmailVerification = false)
        {
            BaseResponse<object> httpResponse = await _mediator.Send(new ResendOtpModelRequest() { Email = email, IsEmailVerification = isEmailVerification });
            return httpResponse;
        }

        [HttpPost("reset-password")]
        public async Task<BaseResponse<object>> ResetPassword([FromBody] ResetPasswordHttpRequest httpRequest)
        {

            var request = _mapper.Map<ResetPasswordModelRequest>(httpRequest);
            BaseResponse<object> httpResponse = await _mediator.Send(request);
            return httpResponse;
        }
    }
}

