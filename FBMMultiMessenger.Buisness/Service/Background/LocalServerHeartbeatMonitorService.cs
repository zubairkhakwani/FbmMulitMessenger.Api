using FBMMultiMessenger.Buisness.Service.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FBMMultiMessenger.Buisness.Service.Background
{
    public class LocalServerHeartbeatMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public LocalServerHeartbeatMonitorService(IServiceProvider serviceProvider)
        {
            this._serviceProvider=serviceProvider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var localServerService = scope.ServiceProvider.GetRequiredService<ILocalServerService>();
                        //TODO: UNCOMMENT
                        //await localServerService.MonitorHeartBeatAsync();
                    }

                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("LocalServerHeartbeatMonitorService was cancelled before execution.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred in LocalServerHeartbeatMonitorService.", ex.Message);
            }
        }
    }
}
