using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class GetAccountsStatusModelRequestHandler : IRequestHandler<GetAccountsStatusModelRequest, BaseResponse<GetAccountsStatusModelResponse>>
    {
        private readonly ApplicationDbContext dbContext;
        private readonly CurrentUserService currentUserService;

        public GetAccountsStatusModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this.dbContext = dbContext;
            this.currentUserService = currentUserService;
        }

        public async Task<BaseResponse<GetAccountsStatusModelResponse>> Handle(GetAccountsStatusModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = currentUserService.GetCurrentUser();

            // Pull the DB's stored bit, but resolve the final value from the live SignalR connection — the
            // batched status-flush can leave IsExtensionConnected stale (connected extension shown offline).
            // Any DB/live drift is reported to Sentry (throttled) by the resolver.
            var accounts = await dbContext.Accounts.Where(a => a.UserId == currentUser.Id)
                .Select(a => new { a.Id, DbConnected = a.IsExtensionConnected })
                .ToListAsync(cancellationToken);

            var statuses = accounts
                .Select(a => new AccountStatusResponse
                {
                    Id = a.Id,
                    IsConnected = ExtensionPresenceResolver.ResolveIsConnected(a.Id, currentUser.Id, a.DbConnected)
                })
                .ToList();

            var response = new GetAccountsStatusModelResponse()
            {
                Statuses = statuses
            };

            return BaseResponse<GetAccountsStatusModelResponse>.Success("", response);
        }
    }
}
