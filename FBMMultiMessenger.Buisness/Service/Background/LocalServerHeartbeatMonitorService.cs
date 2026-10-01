using FBMMultiMessenger.Buisness.Service.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentry;

namespace FBMMultiMessenger.Buisness.Service.Background
{
    public class LocalServerHeartbeatMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public LocalServerHeartbeatMonitorService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            #if DEBUG
                return;
            #endif

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var localServerService = scope.ServiceProvider.GetRequiredService<ILocalServerService>();
                        await localServerService.MonitorHeartBeatAsync();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Benign: app is shutting down before the 2-minute delay elapsed — don't report.
                Console.WriteLine("LocalServerHeartbeatMonitorService was cancelled before execution.");
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine("An error occurred in LocalServerHeartbeatMonitorService.", ex.Message);
            }
        }
    }
}
