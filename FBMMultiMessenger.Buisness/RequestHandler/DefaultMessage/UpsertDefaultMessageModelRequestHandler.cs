using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
using FBMMultiMessenger.Buisness.Request.DefaultMessage;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.DefaultMessage
{
    internal class UpsertDefaultMessageModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, ISignalRService _signalRService) : IRequestHandler<UpsertDefaultMessageModelRequest, BaseResponse<UpsertDefaultMessageModelResponse>>
    {
        public async Task<BaseResponse<UpsertDefaultMessageModelResponse>> Handle(UpsertDefaultMessageModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

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
            {
                return BaseResponse<UpsertDefaultMessageModelResponse>.Error("Default message does not exist");
            }

            defaultMessage.Message = request.Message;

            defaultMessage.ApplyToUpcomingAccounts = request.ApplyToUpcomingAccounts;

            defaultMessage.UpdatedAt = DateTime.UtcNow;

            if (request.ApplyToUpcomingAccounts)
            {
                await UpcomingDefaultMessageHelper.ClearOtherUpcomingFlagsAsync(_dbContext, request.CurrentUserId, defaultMessage.Id, cancellationToken);

                // Upcoming is not linked — clear any previous specific links on this DM.
                var serverMessagesDTO = UnlinkAllAccounts(defaultMessage);

                await _dbContext.SaveChangesAsync(cancellationToken);

                await NotifyServersAsync(serverMessagesDTO, cancellationToken);

                return BaseResponse<UpsertDefaultMessageModelResponse>.Success("Successfully updated default message", new UpsertDefaultMessageModelResponse());
            }

            var currentAccounts = defaultMessage.Accounts.ToList();

            var specificDto = new LocalServerAccountDefaultMessage();

            var unselectedAccounts = currentAccounts
                                                    .Where(account => !request.SelectedAccounts.Contains(account.Id))
                                                    .ToList();

            foreach (var account in unselectedAccounts)
            {
                account.DefaultMessageId = null;

                if (account.LocalServer is null)
                {
                    continue;
                }

                var serverUniqueId = account.LocalServer.UniqueId;

                specificDto.AccountDefaultMessages.TryAdd(serverUniqueId, new());

                specificDto.AccountDefaultMessages[serverUniqueId].Add(new()
                {
                    FbAccountId = account.FbAccountId,
                    DefaultMessage = null
                });
            }

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
                    specificDto.AccountDefaultMessages.TryAdd(serverUniqueId, new());
                    specificDto.AccountDefaultMessages[serverUniqueId].Add(new()
                    {
                        FbAccountId = account.FbAccountId,
                        DefaultMessage = request.Message
                    });
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await NotifyServersAsync(specificDto, cancellationToken);

            return BaseResponse<UpsertDefaultMessageModelResponse>.Success("Successfully updated default message", new UpsertDefaultMessageModelResponse());
        }

        public async Task<BaseResponse<UpsertDefaultMessageModelResponse>> AddRequestAsync(UpsertDefaultMessageModelRequest request, CancellationToken cancellationToken)
        {
            var currentUserId = request.CurrentUserId;
            var newDefaultMessage = new Data.Database.DbModels.DefaultMessage()
            {
                Message = request.Message,
                UserId = currentUserId,
                ApplyToUpcomingAccounts = request.ApplyToUpcomingAccounts,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _dbContext.DefaultMessages.AddAsync(newDefaultMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (request.ApplyToUpcomingAccounts)
            {
                await UpcomingDefaultMessageHelper.ClearOtherUpcomingFlagsAsync(_dbContext, currentUserId, newDefaultMessage.Id, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                // No account links — message is resolved at chat / local-server time.
                return BaseResponse<UpsertDefaultMessageModelResponse>.Success("Successfully added default message", new UpsertDefaultMessageModelResponse());
            }

            var userAccounts = await _dbContext.Accounts
                                               .Include(ls => ls.LocalServer)
                                               .Where(x => x.UserId == currentUserId && x.IsActive)
                                               .ToListAsync(cancellationToken);

            var serverMessagesDTO = new LocalServerAccountDefaultMessage();

            foreach (var accountId in request.SelectedAccounts)
            {
                var selectedAccount = userAccounts.FirstOrDefault(x => x.Id == accountId);

                if (selectedAccount is null)
                {
                    continue;
                }

                if (selectedAccount.DefaultMessageId != null)
                {
                    continue;
                }

                selectedAccount.DefaultMessageId = newDefaultMessage.Id;

                if (selectedAccount.LocalServer is null)
                {
                    continue;
                }

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

            await NotifyServersAsync(serverMessagesDTO, cancellationToken);

            return BaseResponse<UpsertDefaultMessageModelResponse>.Success("Successfully added default message to your selected accounts", new UpsertDefaultMessageModelResponse());
        }

        private static LocalServerAccountDefaultMessage UnlinkAllAccounts(
            Data.Database.DbModels.DefaultMessage defaultMessage)
        {
            var serverMessagesDTO = new LocalServerAccountDefaultMessage();

            foreach (var account in defaultMessage.Accounts.ToList())
            {
                account.DefaultMessageId = null;

                if (account.LocalServer is null)
                {
                    continue;
                }

                var serverUniqueId = account.LocalServer.UniqueId;
                serverMessagesDTO.AccountDefaultMessages.TryAdd(serverUniqueId, new());
                serverMessagesDTO.AccountDefaultMessages[serverUniqueId].Add(new()
                {
                    FbAccountId = account.FbAccountId,
                    DefaultMessage = null
                });
            }

            return serverMessagesDTO;
        }

        private async Task NotifyServersAsync(LocalServerAccountDefaultMessage serverMessagesDTO, CancellationToken cancellationToken)
        {
            foreach (var serverAccount in serverMessagesDTO.AccountDefaultMessages)
            {
                await _signalRService.NotifyLocalServerUpsertDefaultMessage(
                    serverAccount.Value,
                    serverAccount.Key,
                    cancellationToken);
            }
        }
    }
}
