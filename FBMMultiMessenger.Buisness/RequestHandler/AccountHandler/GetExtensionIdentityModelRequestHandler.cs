using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class GetExtensionIdentityModelRequestHandler : IRequestHandler<GetExtensionIdentityModelRequest, BaseResponse<GetExtensionIdentityModelResponse>>
    {
        private readonly CurrentUserService _currentUserService;

        public GetExtensionIdentityModelRequestHandler(CurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public Task<BaseResponse<GetExtensionIdentityModelResponse>> Handle(GetExtensionIdentityModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser == null)
            {
                return Task.FromResult(BaseResponse<GetExtensionIdentityModelResponse>.Error("Unauthorized."));
            }

            return Task.FromResult(BaseResponse<GetExtensionIdentityModelResponse>.Success("Identity resolved.", new GetExtensionIdentityModelResponse { UserId = currentUser.Id }));


        }
    }
}
