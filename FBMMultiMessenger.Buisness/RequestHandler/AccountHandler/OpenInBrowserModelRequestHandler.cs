using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Enums;
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
                                          .Include(ls => ls.LocalServer)
                                          .Include(u => u.User)
                                          .ThenInclude(ls => ls.LocalServers)
                                          .FirstOrDefaultAsync(x => x.Id == request.AccountId
                                                         &&
                                                         x.UserId == currentUserId, cancellationToken);

            if (account is null)
            {
                return BaseResponse<object>.Error("Invalid request, Account does not exist.");
            }

            var accountLocalServer = account.LocalServer;

            if (accountLocalServer is not null &&  account.Status == AccountStatus.Active)
            {
                return BaseResponse<object>.Error("Account is already active and running.");
            }

            var newAccountHttpResponse = new AccountDTO()
            {
                Id =  account.Id,
                Name = account.Name,
                Cookie = account.Cookie,
                IsBrowserOpenRequest = true,
                CreatedAt = account.CreatedAt
            };

            var userLocalServers = account.User.LocalServers;

            var powerFullSystem = LocalServerHelper.GetAvailablePowerfulServer(userLocalServers);

            if (powerFullSystem is null)
            {
                return BaseResponse<object>.Error("No available server found to run the account.");
            }

            account.Status = AccountStatus.InProgress;
            account.LocalServerId = powerFullSystem.Id;

            await _dbContext.SaveChangesAsync(cancellationToken);

            //Inform our console app to open browser if not opened.
            await _hubContext.Clients.Group($"{powerFullSystem.UniqueId}")
               .SendAsync("HandleUpsertAccount", newAccountHttpResponse, cancellationToken);


            return BaseResponse<object>.Success("Account is being opened in the browser", new object());
        }
    }
}
