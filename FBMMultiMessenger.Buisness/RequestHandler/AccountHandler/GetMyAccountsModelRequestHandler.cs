using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Extensions;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class GetMyAccountsModelRequestHandler : IRequestHandler<GetMyAccountsModelRequest, BaseResponse<UserAccountsOverviewModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetMyAccountsModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<UserAccountsOverviewModelResponse>> Handle(GetMyAccountsModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currenuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<UserAccountsOverviewModelResponse>.Error("Invalid Request, Please login again to continue.");
            }

            var accounts = _dbContext.Accounts
                                     .Include(p => p.Proxy)
                                     .Include(dm => dm.DefaultMessage)
                                     .AsNoTracking()
                                     .Where(x => x.UserId == currentUser.Id && x.IsActive);

            var userAccounts = await accounts
                                     .Skip((request.PageNo - 1) * request.PageSize)
                                     .Take(request.PageSize)
                                        .Select(x => new UserAccountsModelResponse()
                                        {
                                            Id = x.Id,
                                            Name = x.Name,
                                            Cookie =  x.Cookie,
                                            DefaultMessage = x.DefaultMessage != null ? x.DefaultMessage.Message : null,
                                            ConnectionStatus = x.ConnectionStatus.GetInfo().Name,
                                            AuthStatus = x.AuthStatus.GetInfo().Name,
                                            CreatedAt = x.CreatedAt,
                                            Proxy = x.Proxy == null ? null : new AccountProxyModelResponse()
                                            {
                                                Id = x.Proxy.Id,
                                                Name = x.Proxy.Name,
                                                Ip_Port = x.Proxy.Ip_Port,
                                            }
                                        })
                                        .OrderBy(x => x.Id)
                                        .ToListAsync(cancellationToken);

            //Apply filter
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.ToLower().Trim();

                userAccounts = userAccounts.Where(x => x.Name.Trim().ToLower().Contains(keyword)).ToList();
            }

            var totalCount = accounts.Count();
            var connectedAccounts = accounts.Count(x => x.AuthStatus == AccountAuthStatus.LoggedIn);
            var notConnectedAccounts = accounts.Count(x => x.AuthStatus != AccountAuthStatus.LoggedIn);

            var pageableUserAccounts = new PageableResponse<UserAccountsModelResponse>(userAccounts, request.PageNo, request.PageSize, totalCount, totalCount/request.PageSize);
            var response = new UserAccountsOverviewModelResponse()
            {
                UserAccounts = pageableUserAccounts,
                ConnectedAccounts = connectedAccounts,
                NotConnectedAccounts = notConnectedAccounts,

            };

            return BaseResponse<UserAccountsOverviewModelResponse>.Success("Operation performed successfully", response);
        }
    }
}
