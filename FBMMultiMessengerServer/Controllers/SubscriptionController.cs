using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Request.Subscription;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Subscription;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public SubscriptionController(IMediator mediator, IMapper mapper)
        {
            this._mediator=mediator;
            this._mapper=mapper;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<GetMySubscriptionHttpResponse>> GetMySubscription()
        {
            GetMySubscriptionModelRequest request = new GetMySubscriptionModelRequest()
            {
                UserId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value)
            };

            BaseResponse<GetMySubscriptionModelResponse> response = await _mediator.Send(request);

            BaseResponse<GetMySubscriptionHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetMySubscriptionHttpResponse>>(response);

            return httpResponse;
        }

    }
}
