using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace FBMMultiMessenger.Buisness.Service.Background
{
    internal class ChatMediaCleanupService : BackgroundService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan MaxFileAge = TimeSpan.FromMinutes(15);

        public ChatMediaCleanupService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupDirectory();
                }
                catch (Exception ex)
                {

                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        private void CleanupDirectory()
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string filePath = @"Images\ChatMessages";
            string mediaRootPath = Path.Combine(wwwRootPath, filePath);

            if (!Directory.Exists(mediaRootPath))
            {
                return;
            }

            var now = DateTime.UtcNow;

            var files = Directory.EnumerateFiles(mediaRootPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);

                    if ((now - fileInfo.CreationTimeUtc) < MaxFileAge)
                        continue;

                    File.Delete(file);
                }
                catch (IOException)
                {
                    // File may be in use — skip safely
                }
                catch (UnauthorizedAccessException)
                {
                    // Permissions or locked file — skip safely
                }
            }
        }
    }
}
