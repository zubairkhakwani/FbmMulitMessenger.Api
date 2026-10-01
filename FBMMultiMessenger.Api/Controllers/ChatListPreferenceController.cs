using AutoMapper;
using FBMMultiMessenger.Buisness.Request.ChatListPreference;
using FBMMultiMessenger.Contracts.Contracts.ChatListPreference;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatListPreferenceController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ChatListPreferenceController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<GetChatListPreferenceHttpResponse>> GetMine()
        {
            var response = await _mediator.Send(new GetChatListPreferenceModelRequest());
            return _mapper.Map<BaseResponse<GetChatListPreferenceHttpResponse>>(response);
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<BaseResponse<GetChatListPreferenceHttpResponse>> UpsertMine(
            [FromBody] UpsertChatListPreferenceHttpRequest httpRequest)
        {
            var request = _mapper.Map<UpsertChatListPreferenceModelRequest>(httpRequest);
            var response = await _mediator.Send(request);
            return _mapper.Map<BaseResponse<GetChatListPreferenceHttpResponse>>(response);
        }
    }
}
