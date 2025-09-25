using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Threading.Tasks;

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
        [HttpPost("receive")]
        public async Task<BaseResponse<ReceiveChatHttpResponse>> ReceiveChat([FromBody] ReceiveChatHttpRequest httpRequest)
        {
            ReceiveChatModelRequest request = _mapper.Map<ReceiveChatModelRequest>(httpRequest);
            var userId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value);
            request.UserId = userId;

            BaseResponse<ReceiveChatModelResponse> response = await _mediator.Send(request);

            BaseResponse<ReceiveChatHttpResponse> httpResponse = _mapper.Map<BaseResponse<ReceiveChatHttpResponse>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpGet("{chatId}/chatmessages")]
        public async Task<BaseResponse<List<GeChatMessagesHttpResponse>>> GetMyChatMessages([FromRoute] int chatId)
        {
            GetChatMessagesModelRequest request = new GetChatMessagesModelRequest()
            {
                ChatId = chatId,
                UserId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value)
            };

            BaseResponse<List<GetChatMessagesModelResponse>> response = await _mediator.Send(request);

            BaseResponse<List<GeChatMessagesHttpResponse>> httpResponse = _mapper.Map<BaseResponse<List<GeChatMessagesHttpResponse>>>(response);

            return httpResponse;
        }


        [Authorize]
        [HttpPost("send")]
        public async Task<BaseResponse<SendChatMessageHttpResponse>> ReceiveChatMessage([FromBody] SendChatMessageHttpRequest httpRequest)
        {
            SendChatMessageModelRequest request = _mapper.Map<SendChatMessageModelRequest>(httpRequest);
            var userId = Convert.ToInt32(HttpContext.User.FindFirst("Id")?.Value);
            request.UserId = userId;

            BaseResponse<SendChatMessageModelResponse> response = await _mediator.Send(request);

            BaseResponse<SendChatMessageHttpResponse> httpResponse = _mapper.Map<BaseResponse<SendChatMessageHttpResponse>>(response);

            return httpResponse;
        }
    }
}
