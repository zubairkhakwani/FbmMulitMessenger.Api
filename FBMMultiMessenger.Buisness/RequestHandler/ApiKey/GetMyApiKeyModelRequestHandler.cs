using FBMMultiMessenger.Buisness.Request.ApiKey;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.ApiKey
{
    internal class GetMyApiKeyModelRequestHandler : IRequestHandler<GetMyApiKeyModelRequest, BaseResponse<GetMyApiKeyModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetMyApiKeyModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<GetMyApiKeyModelResponse>> Handle(GetMyApiKeyModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return BaseResponse<GetMyApiKeyModelResponse>.Error("Invalid request, please login again to continue");
            }

            var user = await _dbContext.Users
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(x => x.Id == currentUser.Id, cancellationToken);

            if (user is null)
            {
                return BaseResponse<GetMyApiKeyModelResponse>.Error("Invalid request, please login again to continue");
            }

            if (string.IsNullOrWhiteSpace(user.ApiKey))
            {
                return BaseResponse<GetMyApiKeyModelResponse>.Success("You have not generated an API key yet", null);
            }

            // Metadata comes from the active audit row; the live secret is Users.ApiKey.
            var activeApiKey = await _dbContext.ApiKeys
                                               .AsNoTracking()
                                               .Where(x => x.UserId == currentUser.Id && x.IsActive)
                                               .OrderByDescending(x => x.CreatedAt)
                                               .FirstOrDefaultAsync(cancellationToken);

            var record = new GetMyApiKeyModelResponse()
            {
                Id = activeApiKey?.Id ?? 0,
                Key = user.ApiKey,
                IsActive = true,
                CreatedAt = activeApiKey?.CreatedAt ?? user.CreatedAt,
                UpdatedAt = activeApiKey?.UpdatedAt ?? activeApiKey?.CreatedAt ?? user.CreatedAt
            };

            return BaseResponse<GetMyApiKeyModelResponse>.Success("Operation performed successfully", record);
        }
    }
}
