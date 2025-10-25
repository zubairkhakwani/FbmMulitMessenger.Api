using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Request.Profile;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Profile;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ProfileController(IMediator mediator, IMapper mapper)
        {
            _mediator=mediator;
            _mapper=mapper;
        }


        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<GetMyProfileHttpResponse>> GetProfile()
        {
            BaseResponse<GetMyProfileModelResponse> response = await _mediator.Send(new GetMyProfileModelRequest());
            BaseResponse<GetMyProfileHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetMyProfileHttpResponse>>(response);
            return httpResponse;
        }

        [Authorize]
        [HttpPut("me/edit")]
        public async Task<BaseResponse<object>> Edit([FromBody] EditProfileHttpRequest httpRequest)
        {
            EditProfileModelRequest request = _mapper.Map<EditProfileModelRequest>(httpRequest);
            BaseResponse<object> httpResponse = await _mediator.Send(request);
            return httpResponse;
        }

        [Authorize]
        [HttpPut("me/changepassword")]
        public async Task<BaseResponse<object>> ChangePassword([FromBody] ChangePasswordHttpRequest httpRequest)
        {
            ChangePasswordModelRequest request = _mapper.Map<ChangePasswordModelRequest>(httpRequest);
            BaseResponse<object> httpResponse = await _mediator.Send(request);
            return httpResponse;
        }
    }
}
