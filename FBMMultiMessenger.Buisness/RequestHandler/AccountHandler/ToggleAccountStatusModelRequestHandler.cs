using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.cs.AccountHandler
{
    internal class ToggleAccountStatusModelRequestHandler : IRequestHandler<ToggleAcountStatusModelRequest, BaseResponse<ToggleAcountStatusModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;

        public ToggleAccountStatusModelRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> _hubContext)
        {
            this._dbContext=dbContext;
            this._hubContext=_hubContext;
        }
        public async Task<BaseResponse<ToggleAcountStatusModelResponse>> Handle(ToggleAcountStatusModelRequest request, CancellationToken cancellationToken)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.UserId == request.UserId && x.Id == request.AccountId);

            if (account is null)
            {
                return BaseResponse<ToggleAcountStatusModelResponse>.Error("Account does not exist");
            }

            var actionPerformed = account.IsActive ? "deactivated" : "activated";
            account.IsActive = !account.IsActive;


            _dbContext.Update(account);
            await _dbContext.SaveChangesAsync();


            var accountDTO = new ConsoleAccountDTO()
            {
                Id =  account.Id,
                Name = account.Name,
                Cookie = account.Cookie,
                IsActive = account.IsActive
            };


            //Inform our console app to close/re-open browser accordingly.
            await _hubContext.Clients.Group(request.UserId.ToString())
               .SendAsync("HandleAccountToggle", accountDTO, cancellationToken);


            return BaseResponse<ToggleAcountStatusModelResponse>.Success($"Account has been {actionPerformed}", new ToggleAcountStatusModelResponse());
        }
    }
}
