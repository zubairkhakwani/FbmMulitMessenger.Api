using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.StatusBatching;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    public class UpdateAccountStatusFromExtensionRequestHandler : IRequestHandler<UpdateAccountStatusFromExtensionRequest, BaseResponse<UpdateAccountStatusFromExtensionResponse>>
    {
        private readonly CurrentUserService currentUserService;
        private readonly IAccountStatusQueue accountStatusQueue;

        public UpdateAccountStatusFromExtensionRequestHandler(CurrentUserService currentUserService, IAccountStatusQueue accountStatusQueue)
        {
            this.currentUserService = currentUserService;
            this.accountStatusQueue = accountStatusQueue;
        }

        public Task<BaseResponse<UpdateAccountStatusFromExtensionResponse>> Handle(UpdateAccountStatusFromExtensionRequest request, CancellationToken cancellationToken)
        {
            var currentUser = currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return Task.FromResult(BaseResponse<UpdateAccountStatusFromExtensionResponse>.Error("Please login again to continue"));
            }

            // Auth path owns only the auth dimension (AuthStatus + Reason). Presence (ConnectionStatus /
            // IsExtensionConnected) is intentionally left untouched so it can't clobber the hub-owned
            // online/offline state. The change is batched and applied by AccountStatusFlushService, which
            // also enforces that the account belongs to this user.
            accountStatusQueue.Enqueue(new AccountStatusChange
            {
                AccountId = request.AccountId,
                UserId = currentUser.Id,
                AuthStatus = request.AccountAuthStatus,
                Reason = request.Reason,
                AtUtc = DateTime.UtcNow,
            });

            return Task.FromResult(BaseResponse<UpdateAccountStatusFromExtensionResponse>.Success("", new()));
        }
    }
}
