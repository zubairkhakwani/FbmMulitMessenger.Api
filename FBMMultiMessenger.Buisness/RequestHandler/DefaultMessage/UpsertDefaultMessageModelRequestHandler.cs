using Azure.Core;
using FBMMultiMessenger.Buisness.Request.DefaultMessage;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;

namespace FBMMultiMessenger.Buisness.RequestHandler.DefaultMessage
{
    internal class UpsertDefaultMessageModelRequestHandler : IRequestHandler<UpsertDefaultMessageModelRequest, BaseResponse<UpsertDefaultMessageModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public UpsertDefaultMessageModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<UpsertDefaultMessageModelResponse>> Handle(UpsertDefaultMessageModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check, if user has came to this point he will be logged in hence currentUser can never be null.
            if (currentUser is null)
            {
                return BaseResponse<UpsertDefaultMessageModelResponse>.Error("Invalid request, Please login again to continue");
            }

            request.CurrentUserId = currentUser.Id;

            if (request.Id is null)
            {
                return await AddRequestAsync(request, cancellationToken);
            }

            return await UpdateRequestAsync(request, cancellationToken);
        }

        public async Task<BaseResponse<UpsertDefaultMessageModelResponse>> UpdateRequestAsync(UpsertDefaultMessageModelRequest request, CancellationToken cancellationToken)
        {
            var defaultMessage = await _dbContext.DefaultMessages
                                                 .Include(x => x.Accounts)
                                                     .FirstOrDefaultAsync(x => x.Id == request.Id
                                                                          &&
                                                                          x.UserId == request.CurrentUserId, cancellationToken
                                                     );

            if (defaultMessage is not null)
            {
                var accounts = defaultMessage.Accounts;
                defaultMessage.Message = request.Message;

                var unSelectedAccounts = accounts.Where(x => !request.SelectedAccounts.Any(y => x.Id == y)).ToList();

                foreach (var account in unSelectedAccounts)
                {
                    if (account.DefaultMessageId is not null)
                    {
                        account.DefaultMessageId = null;
                    }
                }

                var newSelectedAccountsIds = request.SelectedAccounts
                                                    .Where(x => !accounts.Any(y => y.Id == x))
                                                    .ToList();


                foreach (var accountId in newSelectedAccountsIds)
                {
                    var userAccount = await _dbContext.Accounts
                                                .FirstOrDefaultAsync(x => x.Id == accountId
                                                                     &&
                                                                     x.UserId == request.CurrentUserId, cancellationToken);

                    if (userAccount is not null && userAccount.DefaultMessageId is null)
                    {
                        userAccount.DefaultMessageId = request.Id;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return BaseResponse<UpsertDefaultMessageModelResponse>.Success("Successfully updated default message", new UpsertDefaultMessageModelResponse());
        }

        public async Task<BaseResponse<UpsertDefaultMessageModelResponse>> AddRequestAsync(UpsertDefaultMessageModelRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = request.CurrentUserId;
            var newDefaultMessage = new Data.Database.DbModels.DefaultMessage()
            {
                Message = request.Message,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.DefaultMessages.AddAsync(newDefaultMessage);
            await _dbContext.SaveChangesAsync(cancellationToken);



            var allAccountsOfCurrentUser = await _dbContext.Accounts
                                                           .Where(x => x.UserId == currentUserId)
                                                           .ToListAsync(cancellationToken);

            foreach (var accountId in request.SelectedAccounts)
            {
                var selectedAccount = allAccountsOfCurrentUser.FirstOrDefault(x => x.Id == accountId);

                if (selectedAccount is not null)
                {
                    selectedAccount.DefaultMessageId = newDefaultMessage.Id;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<UpsertDefaultMessageModelResponse>.Success("Successfully added default message to your selected accounts", new UpsertDefaultMessageModelResponse());
        }
    }
}
