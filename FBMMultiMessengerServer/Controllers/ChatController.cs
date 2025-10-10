using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ChatController(IMediator mediator, IMapper mapper)
        {
            this._mediator=mediator;
            this._mapper=mapper;
        }


        [Authorize]
        [HttpPost("message")]
        public async Task<BaseResponse<HandleChatHttpResponse>> Receive([FromBody] HandleChatHttpRequest httpRequest)
        {
            HandleChatModelRequest request = _mapper.Map<HandleChatModelRequest>(httpRequest);
            var userId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value);
            request.UserId = userId;

            BaseResponse<HandleChatModelResponse> response = await _mediator.Send(request);

            BaseResponse<HandleChatHttpResponse> httpResponse = _mapper.Map<BaseResponse<HandleChatHttpResponse>>(response);

            return httpResponse;
        }


        [Authorize]
        [HttpGet("{fbChatId}/chatmessages")]
        public async Task<BaseResponse<List<GeChatMessagesHttpResponse>>> GetMyChatMessages([FromRoute] string fbChatId)
        {
            GetChatMessagesModelRequest request = new GetChatMessagesModelRequest()
            {
                FbChatId = fbChatId,
                UserId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value)
            };

            BaseResponse<List<GetChatMessagesModelResponse>> response = await _mediator.Send(request);

            BaseResponse<List<GeChatMessagesHttpResponse>> httpResponse = _mapper.Map<BaseResponse<List<GeChatMessagesHttpResponse>>>(response);

            return httpResponse;
        }
    }
}
