using FBMMultiMessenger.Buisness.Request.FacebookWebSocket;
using FBMMultiMessenger.Buisness.Request.Subscription;
using FBMMultiMessenger.Contracts.Contracts.Subscription;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SyncController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize]
        [HttpPost]
        public async Task<BaseResponse<WebSocketModelResponse>> Sync([FromBody] WebSocketModelRequest request)
        {
            var response = await _mediator.Send(request);

            return response;
        }

        [Authorize]
        [HttpPost("listing-info")]
        public async Task SyncListingInfo([FromBody] SyncListingInfoModelRequest request)
        {
            await _mediator.Send(request);
        }
    }
}
