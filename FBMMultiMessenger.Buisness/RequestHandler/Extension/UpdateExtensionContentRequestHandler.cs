using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FBMMultiMessenger.Buisness.RequestHandler.Extension
{
    internal class UpdateExtensionContentRequestHandler(AesEncryptionHelper aesEncryptionHelper, IHubContext<ChatHub> hubContext, ApplicationDbContext dbContext, ExtensionContentCache cache, ExtensionZipCache zipCache, IConfiguration configuration) : IRequestHandler<UpdateExtensionContentRequest, BaseResponse<GetEncExtensionContentModelResponse>>
    {
        private readonly AesEncryptionHelper _aesEncryptionHelper = aesEncryptionHelper;
        private readonly IHubContext<ChatHub> _hubContext = hubContext;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly ExtensionContentCache _cache = cache;
        private readonly ExtensionZipCache _zipCache = zipCache;
        private readonly IConfiguration _configuration = configuration;

        public async Task<BaseResponse<GetEncExtensionContentModelResponse>> Handle(UpdateExtensionContentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var settings = await _dbContext.Settings.FirstOrDefaultAsync(cancellationToken);

                string extensionVersion;
                if (settings == null)
                {
                    settings = new Settings()
                    {
                        Extension_Version = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _dbContext.Settings.AddAsync(settings, cancellationToken);
                    extensionVersion = settings.Extension_Version;
                }
                else
                {
                    extensionVersion = settings.Extension_Version = Guid.NewGuid().ToString();
                    settings.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Re-obfuscate the latest extension files and refresh the cache so the next
                // GET returns the newest content.
                string encryptedExtensionFiles = await _cache.RebuildAsync(extensionVersion, _aesEncryptionHelper);

                // Rebuild the public download ZIP too, so the manual-install package stays in sync.
                var publicApiUrl = _configuration["ApiSettings:ExtensionPublicApiUrl"] ?? "https://api.fbmmessenger.com";
                await _zipCache.RebuildAsync(publicApiUrl);

                var response = new GetEncExtensionContentModelResponse()
                {
                    Css = encryptedExtensionFiles
                };

                // Inform all local servers that the extension files have changed.
                await _hubContext.Clients.Group("AllServers")
                   .SendAsync("HandleExtensionFilesChanged", response, cancellationToken);

                return BaseResponse<GetEncExtensionContentModelResponse>.Success("Successfully updated extension content", response);
            }
            catch (Exception ex)
            {
                return BaseResponse<GetEncExtensionContentModelResponse>.Error("Something went wrong while updating extension content.");
            }
        }
    }
}
