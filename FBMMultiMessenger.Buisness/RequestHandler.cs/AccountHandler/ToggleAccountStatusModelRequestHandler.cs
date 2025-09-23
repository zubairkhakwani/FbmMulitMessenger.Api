using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.DB;
using MediatR;
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

        public ToggleAccountStatusModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
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

            return BaseResponse<ToggleAcountStatusModelResponse>.Success($"Account has been {actionPerformed}", new ToggleAcountStatusModelResponse());
        }
    }
}
