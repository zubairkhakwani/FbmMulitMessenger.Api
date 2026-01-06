using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using Microsoft.AspNetCore.SignalR;
using static FBMMultiMessenger.Buisness.Models.SignalR.LocalServer.LocalServerAccountDefaultMessage;

namespace FBMMultiMessenger.Buisness.Service
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        public SignalRService(IHubContext<ChatHub> hubContext)
        {
            this._hubContext = hubContext;
        }

        #region LocalServer
        public async Task NotifyLocalServerAccountAdded(LocalServerAccountDTO accountDTO, string localServerId, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.Group($"{localServerId}")
                      .SendAsync("HandleAccountAdd", accountDTO, cancellationToken);
        }

        public async Task NotifyLocalServerAccountDeleted(List<LocalServerCloseAccountRequest> serverCloseAccounts, CancellationToken cancellationToken)
        {
            foreach (var server in serverCloseAccounts)
            {
                await _hubContext.Clients.Group($"{server.ServerId}")
                                         .SendAsync("HandleAccountRemoval", server.Accounts, cancellationToken);
            }
        }

        public async Task NotifyLocalServerAccountUpdated(LocalServerAccountDTO accountDTO, string localServerId, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.Group($"{localServerId}")
              .SendAsync("HandleAccountUpdate", accountDTO, cancellationToken);
        }

        public async Task NotifyLocalServerAccountConnect(LocalServerAccountDTO accountDTO, string localServerId, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.Group($"{localServerId}")
                    .SendAsync("HandleAccountConnect", accountDTO, cancellationToken);
        }

        public async Task NotifyLocalServerUpsertDefaultMessage(List<DefaultMessageDTO> defaultMessagesDTO, string localServerId, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.Group($"{localServerId}")
                               .SendAsync("HandleUpsertDefaultMessage", defaultMessagesDTO, cancellationToken);
        }

        public async Task NotifyLocalServerMessageSent(NotifyLocalServer notifyLocalServer, string localServerId, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.Group($"{localServerId}")
            .SendAsync("HandleChatMessage", notifyLocalServer, cancellationToken);
        }

        #endregion


        #region App
        public async Task NotifyAppAccountStatus(List<UserAccountSignalRModel> accountSignalRModels, CancellationToken cancellationToken)
        {
            foreach (var userAccountSignalR in accountSignalRModels)
            {
                var appId = userAccountSignalR.AppId.ToString();
                await _hubContext.Clients.Group(appId)
                .SendAsync("HandleAccountStatus", userAccountSignalR.AccountsStatus, cancellationToken);
            }
        }





        #endregion
    }
}
