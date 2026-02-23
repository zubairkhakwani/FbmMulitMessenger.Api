using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Shared;
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
        [HttpPost("sync-initial")]
        public async Task<BaseResponse<SyncInitialMessagesHttpResponse>> SyncInitialMessages([FromBody] SyncInitialMessagesHttpRequest httpRequest)
        {
            var request = _mapper.Map<SyncInitialMessagesModelRequest>(httpRequest);

            BaseResponse<SyncInitialMessagesModelResponse> response = await _mediator.Send(request);

            BaseResponse<SyncInitialMessagesHttpResponse> httpResponse = _mapper.Map<BaseResponse<SyncInitialMessagesHttpResponse>>(response);

            return httpResponse;
        }


        [Authorize]
        [HttpGet("{chatId}/chatmessages")]
        public async Task<BaseResponse<List<GetChatMessagesHttpResponse>>> GetMyChatMessages([FromRoute] int chatId, CancellationToken cancellationToken = default)
        {
            BaseResponse<List<GetChatMessagesModelResponse>> response = await _mediator.Send(new GetChatMessagesModelRequest() { ChatId = chatId }, cancellationToken);

            BaseResponse<List<GetChatMessagesHttpResponse>> httpResponse = _mapper.Map<BaseResponse<List<GetChatMessagesHttpResponse>>>(response);

            return httpResponse;
        }

        [Authorize]
        [HttpGet("get-unsynced-messages")]
        public async Task<BaseResponse<GetUnSyncedMessagesModelResponse>> GetUnsyncedMessages([FromQuery] DateTimeOffset? lastSyncedAt, CancellationToken cancellationToken = default)
        {
            var response = await _mediator.Send(new GetUnSyncedMessagesModelRequest() { LastSyncedMessageAt = lastSyncedAt }, cancellationToken);

            return response;
        }
    }
}
