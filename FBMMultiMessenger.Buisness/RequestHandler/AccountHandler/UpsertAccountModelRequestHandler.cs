using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;
using System.Threading;

namespace FBMMultiMessenger.Buisness.RequestHandler.cs.AccountHandler
{
    public class UpsertAccountModelRequestHandler : IRequestHandler<UpsertAccountModelRequest, BaseResponse<UpsertAccountModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;

        public UpsertAccountModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> hubContext)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
            this._hubContext = hubContext;
        }
        public async Task<BaseResponse<UpsertAccountModelResponse>> Handle(UpsertAccountModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra Safety Check, if user has came here he would be logged in hence the current user will never be null.
            if (currentUser is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Invalid request, Please login again to continue");
            }

            request.UserId = currentUser.Id;

            if (request.AccountId is null)
            {
                return await AddRequestAsync(request, cancellationToken);
            }

            return await UpdateRequestAsync(request, cancellationToken);
        }

        public async Task<BaseResponse<UpsertAccountModelResponse>> AddRequestAsync(UpsertAccountModelRequest request, CancellationToken cancellationToken)
        {
            var subscription = await _dbContext.Subscriptions
                                     .FirstOrDefaultAsync(x => x.UserId == request.UserId);

            if (subscription is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Oh Snap, It looks like you don’t have a subscription yet. Please subscribe to continue.", redirectToPackages: true);
            }

            var maxLimit = subscription.MaxLimit;
            var limitUsed = subscription.LimitUsed;

            var response = new UpsertAccountModelResponse();

            if (limitUsed >= maxLimit)
            {
                response.IsLimitExceeded = true;
                return BaseResponse<UpsertAccountModelResponse>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.", result: response);
            }

            var today = DateTime.Now;
            var expiryDate = subscription.ExpiredAt;

            if (today >= expiryDate)
            {
                response.IsSubscriptionExpired = true;
                return BaseResponse<UpsertAccountModelResponse>.Error("Your subscription has expired. Please renew to continue using this feature.", redirectToPackages: true, response);
            }


            var newAccount = new Account()
            {
                UserId =request.UserId,
                Cookie = request.Cookie,
                Name = request.Name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            subscription.LimitUsed++;

            await _dbContext.Accounts.AddAsync(newAccount, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var newAccountHttpResponse = new ConsoleAccountDTO()
            {
                Id =  newAccount.Id,
                Name = newAccount.Name,
                Cookie = newAccount.Cookie,
                IsActive = newAccount.IsActive
            };

            await _hubContext.Clients.Group(request.UserId.ToString())
               .SendAsync("HandleUpsertAccount", newAccountHttpResponse, cancellationToken);

            return BaseResponse<UpsertAccountModelResponse>.Success("Account created successfully", response);
        }

        public async Task<BaseResponse<UpsertAccountModelResponse>> UpdateRequestAsync(UpsertAccountModelRequest request, CancellationToken cancellationToken)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == request.AccountId && x.UserId == request.UserId);

            if (account is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Account does not exist");
            }

            var shouldHandleBrowser = account.Cookie != request.Cookie;


            account.Name = request.Name;
            account.Cookie = request.Cookie;
            account.UpdatedAt = DateTime.Now;

            _dbContext.Accounts.Update(account);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = new UpsertAccountModelResponse();

            if (shouldHandleBrowser)
            {
                var newAccountHttpResponse = new ConsoleAccountDTO()
                {
                    Id =  account.Id,
                    Name = account.Name,
                    Cookie = account.Cookie,
                    IsActive = account.IsActive,
                    IsUpdateRequest = true
                };

                await _hubContext.Clients.Group(request.UserId.ToString())
                   .SendAsync("HandleUpsertAccount", newAccountHttpResponse, cancellationToken);
            }

            return BaseResponse<UpsertAccountModelResponse>.Success("Account updated successfully", response);
        }
    }
}
