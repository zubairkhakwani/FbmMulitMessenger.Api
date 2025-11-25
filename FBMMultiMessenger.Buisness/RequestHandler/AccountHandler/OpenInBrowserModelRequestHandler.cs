using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class OpenInBrowserModelRequestHandler : IRequestHandler<OpenInBrowserModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;

        public OpenInBrowserModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> hubContext)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
            this._hubContext=hubContext;
        }
        public async Task<BaseResponse<object>> Handle(OpenInBrowserModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            var currentUserId = currentUser!.Id;

            var account = await _dbContext.Accounts
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(x => x.Id == request.AccountId
                                                         &&
                                                         x.UserId == currentUserId, cancellationToken);

            if (account is null)
            {
                return BaseResponse<object>.Error("Invalid request, Account does not exist.");
            }


            var newAccountHttpResponse = new AccountDTO()
            {
                Id =  account.Id,
                Name = account.Name,
                Cookie = account.Cookie,
                IsBrowserOpenRequest = true,
                CreatedAt = account.CreatedAt
            };

            //Inform our console app to open browser if not opened.
            var consoleUser = $"LocalServer_{currentUserId}";
            await _hubContext.Clients.Group(consoleUser)
               .SendAsync("HandleUpsertAccount", new List<AccountDTO>() { newAccountHttpResponse }, cancellationToken);

            return BaseResponse<object>.Error("Your request has been proccessed successfully.");
        }
    }
}
