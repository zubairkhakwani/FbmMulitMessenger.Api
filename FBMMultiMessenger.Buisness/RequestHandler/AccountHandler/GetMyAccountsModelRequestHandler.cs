using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
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
        private readonly CurrentUserService _currentUserService;

        public GetMyAccountsModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<List<GetMyAccountsModelResponse>>> Handle(GetMyAccountsModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currenuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<List<GetMyAccountsModelResponse>>.Error("Invlaid Request, Please login again to continue.");
            }

            var accounts = _dbContext.Accounts.Where(x => x.UserId == currentUser.Id);

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
