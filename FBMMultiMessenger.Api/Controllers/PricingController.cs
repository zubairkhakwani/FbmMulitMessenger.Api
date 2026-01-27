using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Pricing;
using FBMMultiMessenger.Contracts.Contracts.Pricing;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PricingController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public PricingController(IMediator mediator, IMapper mapper)
        {
            this._mediator=mediator;
            this._mapper=mapper;
        }

        [HttpGet]
        public async Task<BaseResponse<GetAllPricingHttpResponse>> GetAll()
        {
            var request = await _mediator.Send(new GetAllPricingModelRequest());
            var httpResponse = _mapper.Map<BaseResponse<GetAllPricingHttpResponse>>(request);
            return httpResponse;
        }
    }
}
