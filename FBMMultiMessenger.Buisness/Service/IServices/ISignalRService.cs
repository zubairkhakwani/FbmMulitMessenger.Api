using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Models.SignalR.Extension;
using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using static FBMMultiMessenger.Buisness.Models.SignalR.LocalServer.LocalServerAccountDefaultMessage;

namespace FBMMultiMessenger.Buisness.Service.IServices
{
    public interface ISignalRService
    {

        // Local Server Account Notifications
        Task NotifyLocalServerAccountAdded(LocalServerAccountDTO accountDTO, string localServerId, CancellationToken cancellationToken);
        Task NotifyLocalServerAccountUpdated(LocalServerAccountDTO accountDTO, string localServerId, CancellationToken cancellationToken);
        Task NotifyLocalServerAccountDeleted(List<LocalServerCloseAccountRequest> serverCloseAccounts, CancellationToken cancellationToken);
        Task NotifyLocalServerAccountConnect(LocalServerAccountDTO accountDTO, string localServerId, CancellationToken cancellationToken);
        Task NotifyLocalServerUpsertDefaultMessage(List<DefaultMessageDTO> defaultMessagesDTO, string localServerId, CancellationToken cancellationToken);
        Task NotifyLocalServerMessageSent(NotifyLocalServer notifyLocalServer, string localServerId, CancellationToken cancellationToken);

        //Extension
        Task NotifyExtensionMessageSent(NotifyLocalServer notifyLocalServer, int accountId, CancellationToken cancellationToken);


        // App Account Notifications
        Task NotifyAppAccountStatus(List<UserAccountSignalRModel> accountSignalRModels, CancellationToken cancellationToken);
        Task NotifyAppForMessage(int userId, HandleChatHttpResponse receivedChat, CancellationToken cancellationToken);
        Task NotifyAppChatInfoUpdated(int userId, ChatInfoUpdatedSignalRModel request, CancellationToken cancellationToken);

        Task AskExtensionForListingInfo(int accountId, GetListingInfoRequest request, CancellationToken cancellationToken);

    }
}
