using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    // Kept for direct/MediatR use; the batched path (SyncFlushService) calls SyncMessagesProcessor directly.
    internal class SyncInitialMessagesModelRequestHandler : IRequestHandler<SyncInitialMessagesModelRequest, BaseResponse<SyncInitialMessagesModelResponse>>
    {
        private readonly CurrentUserService currentUserService;
        private readonly SyncMessagesProcessor processor;

        public SyncInitialMessagesModelRequestHandler(CurrentUserService currentUserService, SyncMessagesProcessor processor)
        {
            this.currentUserService = currentUserService;
            this.processor = processor;
        }

        public async Task<BaseResponse<SyncInitialMessagesModelResponse>> Handle(SyncInitialMessagesModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return BaseResponse<SyncInitialMessagesModelResponse>.Error("Invalid Request, Please login again to continue");
            }

            await processor.ProcessAsync(currentUser.Id, request.AccountId, request.FbAccountId, request.Chats, cancellationToken);

            return BaseResponse<SyncInitialMessagesModelResponse>.Success("Synced", new SyncInitialMessagesModelResponse());
        }
    }
}
