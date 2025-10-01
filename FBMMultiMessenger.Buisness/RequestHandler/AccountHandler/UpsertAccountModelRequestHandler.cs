using Azure.Core;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.Database.DbModels;
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
    public class UpsertAccountModelRequestHandler : IRequestHandler<UpsertAccountModelRequest, BaseResponse<UpsertAccountModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public UpsertAccountModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<BaseResponse<UpsertAccountModelResponse>> Handle(UpsertAccountModelRequest request, CancellationToken cancellationToken)
        {
            if (request.AccountId is null)
            {
                return await AddRequestAsync(request);
            }

            return await UpdateRequestAsync(request);
        }

        public async Task<BaseResponse<UpsertAccountModelResponse>> AddRequestAsync(UpsertAccountModelRequest request)
        {
            var subscription = await _dbContext.Subscriptions
                                     .FirstOrDefaultAsync(x => x.UserId == request.UserId);
            if (subscription is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Oops, It looks like you don’t have a subscription yet. Please subscribe to continue.");
            }

            var maxLimit = subscription.MaxLimit;
            var limitUsed = subscription.LimitUsed;

            if (limitUsed>=maxLimit)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.");
            }

            var startDate = subscription.StartedAt;
            var expiryDate = subscription.ExpiredAt;

            if (startDate >= expiryDate || !subscription.IsExpired)
            {
                if (subscription.IsExpired)
                {
                    subscription.IsExpired = false;
                    await _dbContext.SaveChangesAsync();
                }

                return BaseResponse<UpsertAccountModelResponse>.Error("Your subscription has expired. Please renew to continue using this feature.");
            }


            var newAccount = new Account()
            {
                UserId =request.UserId,
                Cookie = request.Cookie,
                Name = request.Name,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            subscription.LimitUsed++;
            await _dbContext.Accounts.AddAsync(newAccount);
            await _dbContext.SaveChangesAsync();

            var response = new UpsertAccountModelResponse() { UserId = newAccount.UserId };

            return BaseResponse<UpsertAccountModelResponse>.Success("Account created successfully", response);

        }

        public async Task<BaseResponse<UpsertAccountModelResponse>> UpdateRequestAsync(UpsertAccountModelRequest request)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == request.AccountId && x.UserId == request.UserId);

            if (account is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Account does not exist");
            }

            account.Name = request.Name;
            account.Cookie = request.Cookie;
            account.UpdatedAt = DateTime.Now;

            _dbContext.Accounts.Update(account);
            await _dbContext.SaveChangesAsync();

            var response = new UpsertAccountModelResponse() { UserId = account.UserId };

            return BaseResponse<UpsertAccountModelResponse>.Success("Account updated successfully", response);
        }
    }
}
