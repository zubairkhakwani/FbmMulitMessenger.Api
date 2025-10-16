using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.cs.AccountHandler
{
    internal class RemoveAccountModelRequestHandler : IRequestHandler<RemoveAcountModelRequest, BaseResponse<ToggleAcountStatusModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;

        public RemoveAccountModelRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> _hubContext)
        {
            this._dbContext=dbContext;
            this._hubContext=_hubContext;
        }
        public async Task<BaseResponse<ToggleAcountStatusModelResponse>> Handle(RemoveAcountModelRequest request, CancellationToken cancellationToken)
        {
            var account = await _dbContext.Accounts
                                          .Include(u => u.User)
                                          .ThenInclude(s => s.Subscriptions)
                                          .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.Id == request.AccountId);

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

            if (activeSubscription is not null)
            {
                activeSubscription.LimitUsed--;
            }

            _dbContext.Remove(account);
            await _dbContext.SaveChangesAsync();


            var accountDTO = new ConsoleAccountDTO()
            {
                Id =  account.Id,
                Name = account.Name,
                Cookie = account.Cookie,
            };

            //Inform our console app to close browser.
            var consoleUser = $"Console_{request.UserId.ToString()}";
            await _hubContext.Clients.Group(consoleUser)
               .SendAsync("HandleAccountRemoval", accountDTO, cancellationToken);


            return BaseResponse<ToggleAcountStatusModelResponse>.Success($"Account has been deleted.", new ToggleAcountStatusModelResponse());
        }
    }
}
