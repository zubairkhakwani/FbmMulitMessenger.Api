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

            var account = await _dbContext.Accounts
                                          .Include(u => u.User)
                                          .ThenInclude(s => s.Subscriptions)
                                          .FirstOrDefaultAsync(x => x.UserId == currentUser.Id
                                                               &&
                                                               x.Id == request.AccountId, cancellationToken);

            if (account is null)
            {
                return BaseResponse<ToggleAcountStatusModelResponse>.Error("Account does not exist");
            }

            var subscriptions = account.User.Subscriptions;
            var today = DateTime.UtcNow;
            var activeSubscription = subscriptions
                                                .Where(x => x.StartedAt <= today
                                                       &&
                                                       x.ExpiredAt > today)
                                                .OrderByDescending(x => x.StartedAt)
                                                .FirstOrDefault();

            if (activeSubscription is not null && activeSubscription.LimitUsed > 0)
            {
                activeSubscription.LimitUsed--; 
            }

            _dbContext.Remove(account);
            await _dbContext.SaveChangesAsync();


            var accountDTO = new AccountDTO()
            {
                Id =  account.Id,
                Name = account.Name,
                Cookie = account.Cookie,
            };

            //Inform our console app to close browser.
            var consoleUser = $"Console_{currentUser.Id.ToString()}";
            await _hubContext.Clients.Group(consoleUser)
               .SendAsync("HandleAccountRemoval", accountDTO, cancellationToken);


            return BaseResponse<ToggleAcountStatusModelResponse>.Success($"Account has been deleted.", new ToggleAcountStatusModelResponse());
        }
    }
}
