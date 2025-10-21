using AutoMapper;
using FBMMultiMessenger.Buisness.Request.DefaultMessage;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.DefaultMessage;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DefaultMessageController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public DefaultMessageController(IMediator mediator, IMapper mapper)
        {
            _mediator=mediator;
            _mapper=mapper;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<GetMyDefaultMessagesHttpResponse>> GetAll()
        {
            BaseResponse<GetMyDefaultMessagesModelResponse> response = await _mediator.Send(new GetMyDefaultMessagesModelRequest());

            BaseResponse<GetMyDefaultMessagesHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetMyDefaultMessagesHttpResponse>>(response);

            return httpResponse;
        }
    }
}
