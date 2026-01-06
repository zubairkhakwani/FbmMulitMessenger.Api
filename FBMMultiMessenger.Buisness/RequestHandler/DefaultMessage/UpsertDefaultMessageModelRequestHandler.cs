using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
using FBMMultiMessenger.Buisness.Request.DefaultMessage;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.DefaultMessage
{
    internal class UpsertDefaultMessageModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, ISignalRService _signalRService) : IRequestHandler<UpsertDefaultMessageModelRequest, BaseResponse<UpsertDefaultMessageModelResponse>>
    {
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
                                                 .ThenInclude(a => a.LocalServer)
                                                 .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == request.CurrentUserId, cancellationToken);

            if (defaultMessage is null)
                return BaseResponse<UpsertDefaultMessageModelResponse>.Error("Default message does not exist");

            // Update the message text
            defaultMessage.Message = request.Message;

            var currentAccounts = defaultMessage.Accounts.ToList();
            var serverMessagesDTO = new LocalServerAccountDefaultMessage();

            // Remove default message from unselected accounts
            var unselectedAccounts = currentAccounts
                                             .Where(account => !request.SelectedAccounts.Contains(account.Id))
                                             .Where(account => account.LocalServer is not null)
                                             .ToList();

            foreach (var account in unselectedAccounts)
            {
                account.DefaultMessageId = null;

                var serverUniqueId = account.LocalServer!.UniqueId;
                serverMessagesDTO.AccountDefaultMessages.TryAdd(serverUniqueId, new());
                serverMessagesDTO.AccountDefaultMessages[serverUniqueId].Add(new()
                {
                    FbAccountId = account.FbAccountId,
                    DefaultMessage = null
                });
            }

            // Add default message to newly selected accounts
            var newlySelectedAccountIds = request.SelectedAccounts
                                                 .Where(id => !currentAccounts.Any(account => account.Id == id))
                                                 .ToList();

            if (newlySelectedAccountIds.Any())
            {
                var newlySelectedAccounts = await _dbContext.Accounts
                                                            .Include(x => x.LocalServer)
                                                            .Where(x => newlySelectedAccountIds.Contains(x.Id)
                                                                && x.UserId == request.CurrentUserId
                                                                && x.DefaultMessageId == null)
                                                            .ToListAsync(cancellationToken);

                foreach (var account in newlySelectedAccounts)
                {
                    account.DefaultMessageId = request.Id;

                    if (account.LocalServer is null) continue;

                    var serverUniqueId = account.LocalServer.UniqueId;
                    serverMessagesDTO.AccountDefaultMessages.TryAdd(serverUniqueId, new());
                    serverMessagesDTO.AccountDefaultMessages[serverUniqueId].Add(new()
                    {
                        FbAccountId = account.FbAccountId,
                        DefaultMessage = request.Message
                    });
                }
            }


            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var serverAccount in serverMessagesDTO.AccountDefaultMessages)
            {
                var serverUniqueId = serverAccount.Key;
                var accountDefaultMessages = serverAccount.Value;

                await _signalRService.NotifyLocalServerUpsertDefaultMessage(accountDefaultMessages, serverUniqueId, cancellationToken);
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

            await _dbContext.DefaultMessages.AddAsync(newDefaultMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var userAccounts = await _dbContext.Accounts
                                               .Include(ls => ls.LocalServer)
                                               .Where(x => x.UserId == currentUserId && x.IsActive)
                                               .ToListAsync(cancellationToken);

            // Prepare DTO to inform local servers about the default message addition
            var serverMessagesDTO = new LocalServerAccountDefaultMessage();

            foreach (var accountId in request.SelectedAccounts)
            {
                var selectedAccount = userAccounts.FirstOrDefault(x => x.Id == accountId);
                if (selectedAccount is null) continue;

                // Assign the newly created default message to the selected account
                selectedAccount.DefaultMessageId = newDefaultMessage.Id;

                if (selectedAccount.LocalServer is null) continue;

                // Group accounts by server unique ID
                var serverUniqueId = selectedAccount.LocalServer.UniqueId;

                if (!serverMessagesDTO.AccountDefaultMessages.ContainsKey(serverUniqueId))
                {
                    serverMessagesDTO.AccountDefaultMessages[serverUniqueId] = new();
                }

                serverMessagesDTO.AccountDefaultMessages[serverUniqueId].Add(new()
                {
                    FbAccountId = selectedAccount.FbAccountId,
                    DefaultMessage = request.Message
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Notify the local server about the new default message
            foreach (var serverAccount in serverMessagesDTO.AccountDefaultMessages)
            {
                var serverUniqueId = serverAccount.Key;
                var accountDefaultMessages = serverAccount.Value;
                await _signalRService.NotifyLocalServerUpsertDefaultMessage(accountDefaultMessages, serverUniqueId, cancellationToken);
            }

            return BaseResponse<UpsertDefaultMessageModelResponse>.Success("Successfully added default message to your selected accounts", new UpsertDefaultMessageModelResponse());
        }
    }
}
