using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.cs.AccountHandler
{
    internal class RemoveAccountModelRequestHandler : IRequestHandler<RemoveAcountModelRequest, BaseResponse<ToggleAcountStatusModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;

        public RemoveAccountModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> _hubContext)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
            this._hubContext=_hubContext;
        }
        public async Task<BaseResponse<ToggleAcountStatusModelResponse>> Handle(RemoveAcountModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currenuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<ToggleAcountStatusModelResponse>.Error("Invalid Request, Please login again to continue.");
            }
            if (request.AccountIds.Count == 0)
            {
                return BaseResponse<ToggleAcountStatusModelResponse>.Error("Invalid Request, Please select any account to delete.");
            }


            var accounts = await _dbContext.Accounts
                                          .Include(u => u.User)
                                          .ThenInclude(s => s.Subscriptions)
                                          .Where(x => x.UserId == currentUser.Id
                                                               &&
                                                 request.AccountIds.Any(id => id == x.Id))
                                          .ToListAsync(cancellationToken);

            var account = accounts?.FirstOrDefault();


            if (accounts == null || account?.User?.Subscriptions == null)
            {
                return BaseResponse<ToggleAcountStatusModelResponse>.Error("Account or subscriptions not found");
            }

            var subscriptions = account.User.Subscriptions;
            var count = accounts.Count;

            var today = DateTime.UtcNow;
            var activeSubscription = subscriptions
                                                .Where(x => x.StartedAt <= today
                                                       &&
                                                       x.ExpiredAt > today)
                                                .OrderByDescending(x => x.StartedAt)
                                                .FirstOrDefault();

            if (activeSubscription is not null && activeSubscription.LimitUsed > 0)
            {
                activeSubscription.LimitUsed -= count;
            }

            _dbContext.RemoveRange(accounts);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var accountDTO = accounts.Select(x => new AccountDTO()
            {
                Id = x.Id,
                Name = x.Name,
                Cookie = x.Cookie,
                CreatedAt = x.CreatedAt,
            });

            //Inform our console app to close browser.
            var consoleUser = $"LocalServer_{currentUser.Id}";
            await _hubContext.Clients.Group(consoleUser)
               .SendAsync("HandleAccountRemoval", accountDTO, cancellationToken);

            var responseMessage = count > 1 ? "Selected accounts" : "Account";

            return BaseResponse<ToggleAcountStatusModelResponse>.Success($"{responseMessage} has been deleted.", new ToggleAcountStatusModelResponse());
        }
    }
}
