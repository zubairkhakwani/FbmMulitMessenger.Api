using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class UpdateAccountCustomNameModelRequestHandler(
        ApplicationDbContext dbContext,
        CurrentUserService currentUserService)
        : IRequestHandler<UpdateAccountCustomNameModelRequest, BaseResponse<UpdateAccountCustomNameModelResponse>>
    {
        public async Task<BaseResponse<UpdateAccountCustomNameModelResponse>> Handle(
            UpdateAccountCustomNameModelRequest request,
            CancellationToken cancellationToken)
        {
            var currentUser = currentUserService.GetCurrentUser();
            var currentUserId = currentUser!.Id;

            if (!TryNormalizeCustomName(request.CustomName, out var customName, out var error))
            {
                return BaseResponse<UpdateAccountCustomNameModelResponse>.Error(error!);
            }

            var account = await dbContext.Accounts
                .FirstOrDefaultAsync(
                    x => x.Id == request.AccountId && x.UserId == currentUserId && x.IsActive,
                    cancellationToken);

            if (account is null)
            {
                return BaseResponse<UpdateAccountCustomNameModelResponse>.Error(
                    "Invalid request, Account does not exist.");
            }

            account.CustomName = customName;
            account.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<UpdateAccountCustomNameModelResponse>.Success(
                "Display name updated successfully",
                new UpdateAccountCustomNameModelResponse
                {
                    AccountId = account.Id,
                    CustomName = customName!,
                });
        }

        /// <summary>
        /// Custom name is required and must be non-empty after trim (max 120).
        /// </summary>
        private static bool TryNormalizeCustomName(
            string? raw,
            out string? customName,
            out string? error)
        {
            customName = null;
            error = null;

            var trimmed = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                error = "Please enter a display name.";
                return false;
            }

            if (trimmed.Length > 120)
            {
                error = "Display name must be 120 characters or fewer.";
                return false;
            }

            customName = trimmed;
            return true;
        }
    }
}
