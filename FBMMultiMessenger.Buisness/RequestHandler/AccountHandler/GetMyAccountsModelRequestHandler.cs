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
    internal class GetMyAccountsModelRequestHandler : IRequestHandler<GetMyAccountsModelRequest, BaseResponse<List<GetMyAccountsModelResponse>>>
    {
        private readonly ApplicationDbContext _dbContext;

        public GetMyAccountsModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<BaseResponse<List<GetMyAccountsModelResponse>>> Handle(GetMyAccountsModelRequest request, CancellationToken cancellationToken)
        {
            var accounts = _dbContext.Accounts.Where(x => x.UserId == request.UserId);

            var response = await accounts.Select(x => new GetMyAccountsModelResponse()
            {
                Id = x.Id,
                Name = x.Name,
                Cookie =  x.Cookie,
                CreatedAt = x.CreatedAt
            }).ToListAsync();

            return BaseResponse<List<GetMyAccountsModelResponse>>.Success("Operation performed successfully", response);
        }
    }
}
