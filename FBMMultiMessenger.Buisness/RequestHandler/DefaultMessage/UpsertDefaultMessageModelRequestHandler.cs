using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.DefaultMessage;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static FBMMultiMessenger.Buisness.Service.CurrentUserService;

namespace FBMMultiMessenger.Buisness.RequestHandler.DefaultMessage
{
    internal class UpsertDefaultMessageModelRequestHandler : IRequestHandler<UpsertDefaultMessageModelRequest, BaseResponse<UpsertDefaultMessageModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;

        public UpsertDefaultMessageModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> hubContext)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
            this._hubContext=hubContext;
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

                // Updating the default message
                defaultMessage.Message = request.Message;

                // Preparing DTO to inform local server about the default message updation
                var defaultMessageDTO = new UpsertDefaultMessageDTO();

                var unSelectedAccounts = accounts.Where(x => !request.SelectedAccounts.Any(y => x.Id == y)).ToList();

                foreach (var account in unSelectedAccounts)
                {
                    if (account.DefaultMessageId is not null)
                    {
                        account.DefaultMessageId = null;
                        // Preparing DTO to inform local server about the default message updation
                        defaultMessageDTO.AccountDefaultMessages.Add(account.FbAccountId,null);
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
                                                                           x.UserId == request.CurrentUserId, cancellationToken
                                                      );

                    if (userAccount is not null && userAccount.DefaultMessageId is null)
                    {
                        userAccount.DefaultMessageId = request.Id;
                        // Preparing DTO to inform local server about the default message updation
                        defaultMessageDTO.AccountDefaultMessages.Add(userAccount.FbAccountId, request.Message);
                    }
                }

                // Notify the local server about the updation of default message
                await _hubContext.Clients.Group($"LocalServer_{request.CurrentUserId}")
               .SendAsync("HandleUpsertDefaultMessage", defaultMessageDTO, cancellationToken);


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

            // Preparing DTO to inform local server about the default message addition
            var defaultMessageDTO = new UpsertDefaultMessageDTO();

            foreach (var accountId in request.SelectedAccounts)
            {
                var selectedAccount = allAccountsOfCurrentUser.FirstOrDefault(x => x.Id == accountId);

                if (selectedAccount is not null)
                {
                    selectedAccount.DefaultMessageId = newDefaultMessage.Id;
                    defaultMessageDTO.AccountDefaultMessages.Add(selectedAccount.FbAccountId,request.Message);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Notify the local server about the new default message
            await _hubContext.Clients.Group($"LocalServer_{currentUserId}")
           .SendAsync("HandleUpsertDefaultMessage", defaultMessageDTO, cancellationToken);

            return BaseResponse<UpsertDefaultMessageModelResponse>.Success("Successfully added default message to your selected accounts", new UpsertDefaultMessageModelResponse());
        }
    }
}
