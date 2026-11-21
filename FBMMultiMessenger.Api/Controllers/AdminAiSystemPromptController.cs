using FBMMultiMessenger.Buisness.Authentication;
using FBMMultiMessenger.Buisness.Request.AiSystemPrompt;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [ApiController]
    public class AdminAiSystemPromptController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminAiSystemPromptController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Reloads Settings.AiSystemPrompt into the API in-memory cache.
        /// Called by the portal after saving the prompt (server-to-server).
        /// </summary>
        [InternalApiKeyAuthorize]
        [HttpPost("/api/admin/ai-system-prompt/reload")]
        public async Task<BaseResponse<ReloadAiSystemPromptModelResponse>> Reload()
        {
            return await _mediator.Send(new ReloadAiSystemPromptModelRequest());
        }
    }
}
