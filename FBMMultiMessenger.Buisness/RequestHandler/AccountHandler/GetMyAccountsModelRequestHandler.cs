using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Extensions;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.cs.AccountHandler
{
    internal class GetMyAccountsModelRequestHandler : IRequestHandler<GetMyAccountsModelRequest, BaseResponse<PageableResponse<GetMyAccountsModelResponse>>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetMyAccountsModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<PageableResponse<GetMyAccountsModelResponse>>> Handle(GetMyAccountsModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currenuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<PageableResponse<GetMyAccountsModelResponse>>.Error("Invlaid Request, Please login again to continue.");
            }

            var accounts = _dbContext.Accounts
                                     .Include(p => p.Proxy)
                                     .Include(dm => dm.DefaultMessage)
                                     .Where(x => x.UserId == currentUser.Id);

            var response = await accounts
                                .Skip((request.PageNo - 1) * request.PageSize)
                                .Take(request.PageSize)
                                .AsNoTracking()
                                .Select(x => new GetMyAccountsModelResponse()
                                {
                                    Id = x.Id,
                                    Name = x.Name,
                                    Cookie =  x.Cookie,
                                    DefaultMessage = x.DefaultMessage != null ? x.DefaultMessage.Message : null,
                                    Status = AccountStatusExtension.GetInfo(x.Status).Name,
                                    CreatedAt = x.CreatedAt,
                                    Proxy = x.Proxy == null ? null : new AccountProxyModelResponse()
                                    {
                                        Id = x.Proxy.Id,
                                        Name = x.Proxy.Name,
                                        Ip_Port = x.Proxy.Ip_Port,
                                    }
                                }).ToListAsync(cancellationToken);

            //Apply filter
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.ToLower().Trim();

                response = response.Where(x => x.Name.Trim().ToLower().Contains(keyword)).ToList();
            }

            var totalCount = accounts.Count();

            var newPageableResponse = new PageableResponse<GetMyAccountsModelResponse>(response, request.PageNo, request.PageSize, totalCount, totalCount/request.PageSize);

            return BaseResponse<PageableResponse<GetMyAccountsModelResponse>>.Success("Operation performed successfully", newPageableResponse);
        }
    }
}
