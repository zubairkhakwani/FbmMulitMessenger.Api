using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.AiSystemPrompt;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;

namespace FBMMultiMessenger.Buisness.RequestHandler.AiSystemPrompt
{
    internal class ReloadAiSystemPromptModelRequestHandler(ApplicationDbContext dbContext, GeminiSystemPromptCache promptCache) : IRequestHandler<ReloadAiSystemPromptModelRequest, BaseResponse<ReloadAiSystemPromptModelResponse>>
    {
        public async Task<BaseResponse<ReloadAiSystemPromptModelResponse>> Handle(ReloadAiSystemPromptModelRequest request, CancellationToken cancellationToken)
        {
            await promptCache.ReloadAsync(dbContext, cancellationToken);

            var prompt = promptCache.Get();

            return BaseResponse<ReloadAiSystemPromptModelResponse>.Success("AI system prompt reloaded into memory", new ReloadAiSystemPromptModelResponse { PromptLength = prompt.Length });
        }
    }
}
