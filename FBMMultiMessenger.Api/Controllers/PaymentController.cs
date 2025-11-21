using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Payment;
using FBMMultiMessenger.Contracts.Contracts.Payment;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public PaymentController(IMediator mediator, IMapper mapper)
        {
            this._mediator=mediator;
            this._mapper=mapper;
        }


        [Authorize]
        [HttpPost("proof")]
        public async Task<BaseResponse<AddPaymentProofHttpResponse>> AddProof([FromForm] AddPaymentProofHttpRequest httpRequest)
        {
            var request = _mapper.Map<AddPaymentProofModelRequest>(httpRequest);
            var response = await _mediator.Send(request);

            var httpResponse = _mapper.Map<BaseResponse<AddPaymentProofHttpResponse>>(response);
            return httpResponse;
        }

        [Authorize]
        [HttpGet("me/status")]
        public async Task<BaseResponse<GetMyVerificationStatusHttpResponse>> GetStatus()
        {
            var response = await _mediator.Send(new GetMyVerificationStatusModelRequest());

            var httpResponse = _mapper.Map<BaseResponse<GetMyVerificationStatusHttpResponse>>(response);
            return httpResponse;
        }

    }
}
