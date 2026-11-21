using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.AiSystemPrompt
{
    public class ReloadAiSystemPromptModelRequest : IRequest<BaseResponse<ReloadAiSystemPromptModelResponse>>
    {
    }

    public class ReloadAiSystemPromptModelResponse
    {
        public int PromptLength { get; set; }
    }
}
