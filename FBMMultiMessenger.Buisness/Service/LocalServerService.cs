using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Data.Database.DbModels;
using MediatR;

namespace FBMMultiMessenger.Buisness.Service
{
    public class LocalServerService : ILocalServerService
    {
        private readonly IMediator _mediator;

        public LocalServerService(IMediator mediator)
        {
            this._mediator=mediator;
        }
        public string GenereatetUniqueId(string systemUUID)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"LocalServer_{systemUUID}-{Guid.NewGuid()}"));
        }

        public LocalServer? GetLeastLoadedServer(List<LocalServer>? servers)
        {
            return servers?
                       .Where(s => s.ActiveBrowserCount < s.MaxBrowserCapacity && s.IsOnline && s.IsActive)
                       .OrderBy(s => s.ActiveBrowserCount)
                       .FirstOrDefault();
        }

        public List<LocalServer>? GetPowerfulServers(List<LocalServer>? servers)
        {
            if (servers is null || servers.Count == 0)
                return null;

            return servers
                        .Where(s => s.IsActive && s.ActiveBrowserCount < s.MaxBrowserCapacity)
                        .OrderByDescending(s => s.TotalMemoryGB)
                        .ThenByDescending(s => s.MaxBrowserCapacity - s.ActiveBrowserCount)
                        .ThenByDescending(s => s.HasSSD)
                        .ThenByDescending(s => s.MaxClockSpeedMHz)
                        .ThenByDescending(s => s.CoreCount).ToList();
        }

        public async Task HandleServerOfflineAsync(string uniqueId)
        {
            await _mediator.Send(new LocalServerDisconnectionModelRequest() { UniqueId = uniqueId });
        }

        public async Task HandleServerOnlineAsync(string uniqueId)
        {
            await _mediator.Send(new LocalServerReconnectionModelRequest() { UniqueId = uniqueId });
        }

        public async Task MonitorHeartBeatAsync()
        {
            await _mediator.Send(new MonitorLocalServerHearbeatModelRequest());
        }
    }
}
