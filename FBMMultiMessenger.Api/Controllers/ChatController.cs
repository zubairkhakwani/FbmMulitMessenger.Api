using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ChatController(IMediator mediator, IMapper mapper)
        {
            _mediator=mediator;
            _mapper=mapper;
        }


        [Authorize]
        [HttpPost("message")]
        public async Task<BaseResponse<HandleChatHttpResponse>> Receive([FromBody] HandleChatHttpRequest httpRequest)
        {
            HandleChatModelRequest request = _mapper.Map<HandleChatModelRequest>(httpRequest);

            BaseResponse<HandleChatModelResponse> response = await _mediator.Send(request);

            BaseResponse<HandleChatHttpResponse> httpResponse = _mapper.Map<BaseResponse<HandleChatHttpResponse>>(response);

            return httpResponse;
        }


        [Authorize]
        [HttpGet("{fbChatId}/chatmessages")]
        public async Task<BaseResponse<List<GeChatMessagesHttpResponse>>> GetMyChatMessages([FromRoute] string fbChatId)
        {
            BaseResponse<List<GetChatMessagesModelResponse>> response = await _mediator.Send(new GetChatMessagesModelRequest() { FbChatId = fbChatId });

            BaseResponse<List<GeChatMessagesHttpResponse>> httpResponse = _mapper.Map<BaseResponse<List<GeChatMessagesHttpResponse>>>(response);

            return httpResponse;
        }
    }
}
