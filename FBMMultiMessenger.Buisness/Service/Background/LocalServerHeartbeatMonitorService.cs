using Microsoft.Extensions.Hosting;
using System.Threading;

namespace FBMMultiMessenger.Buisness.Service.Background
{
    public class LocalServerHeartbeatMonitorService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    //using (var scope = _serviceProvider.CreateScope())
                    //{
                    //    // var myService = scope.ServiceProvider.GetRequiredService<IMyService>();

                    //    // Your actual work here
                    //    _logger.LogInformation("Performing the one-time background task...");

                    //    // Simulate some work
                    //    await Task.Delay(1000, cancellationToken);

                    //    _logger.LogInformation("One-time background task completed successfully.");
                    //}


                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("LocalServerHeartbeatMonitorService was cancelled before execution.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred in OneTimeBackgroundService.", ex.Message);
            }
        }
    }
}
