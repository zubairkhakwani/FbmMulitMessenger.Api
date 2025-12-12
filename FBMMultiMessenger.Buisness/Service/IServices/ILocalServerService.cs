using FBMMultiMessenger.Data.Database.DbModels;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FBMMultiMessenger.Buisness.Service.IServices
{
    public interface ILocalServerService
    {
        List<LocalServer>? GetPowerfulServers(List<LocalServer>? servers);

        LocalServer? GetLeastLoadedServer(List<LocalServer>? servers);

        Task HandleServerOnlineAsync(string uniqueId);
        Task HandleServerOfflineAsync(string uniqueId);
        void MonitorHeartBeatAsync();

        string GenereatetUniqueId(string systemUUID);
    }
}
