using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Subscription;
using FBMMultiMessenger.Contracts.Contracts.Subscription;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public SubscriptionController(IMediator mediator, IMapper mapper)
        {
            _mediator=mediator;
            _mapper=mapper;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<GetMySubscriptionHttpResponse>> GetMySubscription()
        {
            BaseResponse<GetMySubscriptionModelResponse> response = await _mediator.Send(new GetMySubscriptionModelRequest());
            BaseResponse<GetMySubscriptionHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetMySubscriptionHttpResponse>>(response);

            return httpResponse;
        }

    }
}
