using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.ApiKey;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.ApiKey
{
    internal class UpsertApiKeyModelRequestHandler : IRequestHandler<UpsertApiKeyModelRequest, BaseResponse<UpsertApiKeyModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public UpsertApiKeyModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<UpsertApiKeyModelResponse>> Handle(UpsertApiKeyModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return BaseResponse<UpsertApiKeyModelResponse>.Error("Invalid request, Please login again to continue");
            }

            request.CurrentUserId = currentUser.Id;

            if (!request.IsRegenerate)
            {
                return await AddRequestAsync(request, cancellationToken);
            }

            return await UpdateRequestAsync(request, cancellationToken);
        }

        private async Task<BaseResponse<UpsertApiKeyModelResponse>> AddRequestAsync(UpsertApiKeyModelRequest request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                                       .FirstOrDefaultAsync(x => x.Id == request.CurrentUserId, cancellationToken);

            if (user is null)
            {
                return BaseResponse<UpsertApiKeyModelResponse>.Error("Invalid request, Please login again to continue");
            }

            bool hasActiveKey = !string.IsNullOrWhiteSpace(user.ApiKey)
                                || await _dbContext.ApiKeys.AnyAsync(x => x.UserId == request.CurrentUserId && x.IsActive, cancellationToken);

            if (hasActiveKey)
            {
                return BaseResponse<UpsertApiKeyModelResponse>.Error("You already have an API key. Please regenerate it instead.");
            }

            var key = await GenerateUniqueKeyAsync(cancellationToken);
            var now = DateTime.UtcNow;

            var newApiKey = new Data.Database.DbModels.ApiKey()
            {
                Key = key,
                UserId = request.CurrentUserId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            // Users.ApiKey always mirrors the latest active key; ApiKeys stores the audit trail.
            user.ApiKey = key;

            await _dbContext.ApiKeys.AddAsync(newApiKey, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<UpsertApiKeyModelResponse>.Success("API key generated successfully", new UpsertApiKeyModelResponse() { Key = key });
        }

        private async Task<BaseResponse<UpsertApiKeyModelResponse>> UpdateRequestAsync(UpsertApiKeyModelRequest request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                                       .FirstOrDefaultAsync(x => x.Id == request.CurrentUserId, cancellationToken);

            if (user is null)
            {
                return BaseResponse<UpsertApiKeyModelResponse>.Error("Invalid request, Please login again to continue");
            }

            var activeKeys = await _dbContext.ApiKeys
                                             .Where(x => x.UserId == request.CurrentUserId && x.IsActive)
                                             .ToListAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(user.ApiKey) && activeKeys.Count == 0)
            {
                return BaseResponse<UpsertApiKeyModelResponse>.Error("You do not have an API key yet. Please generate one first.");
            }

            var key = await GenerateUniqueKeyAsync(cancellationToken);
            var now = DateTime.UtcNow;

            // Keep previous keys in the audit table; mark them revoked instead of overwriting.
            foreach (var activeKey in activeKeys)
            {
                activeKey.IsActive = false;
                activeKey.RevokedAt = now;
                activeKey.UpdatedAt = now;
            }

            var newApiKey = new Data.Database.DbModels.ApiKey()
            {
                Key = key,
                UserId = request.CurrentUserId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            user.ApiKey = key;

            await _dbContext.ApiKeys.AddAsync(newApiKey, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<UpsertApiKeyModelResponse>.Success("API key regenerated successfully", new UpsertApiKeyModelResponse() { Key = key });
        }

        private async Task<string> GenerateUniqueKeyAsync(CancellationToken cancellationToken)
        {
            string key;

            do
            {
                key = ApiKeyHelper.GenerateKey();
            }
            while (await _dbContext.ApiKeys.AnyAsync(x => x.Key == key, cancellationToken)
                   || await _dbContext.Users.AnyAsync(x => x.ApiKey == key, cancellationToken));

            return key;
        }
    }
}
