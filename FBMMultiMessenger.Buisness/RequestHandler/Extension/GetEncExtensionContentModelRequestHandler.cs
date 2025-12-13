using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FBMMultiMessenger.Buisness.RequestHandler.Extension
{
    internal class GetEncExtensionContentModelRequestHandler(AesEncryptionHelper aesEncryptionHelper, IHubContext<ChatHub> hubContext, ApplicationDbContext dbContext) : IRequestHandler<GetEncExtensionContentModelRequest, BaseResponse<GetEncExtensionContentModelResponse>>
    {
        private readonly AesEncryptionHelper _aesEncryptionHelper = aesEncryptionHelper;
        private readonly IHubContext<ChatHub> _hubContext = hubContext;
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<BaseResponse<GetEncExtensionContentModelResponse>> Handle(GetEncExtensionContentModelRequest request, CancellationToken cancellationToken)
        {
            try
            {
                string baseDir = AppContext.BaseDirectory;
                string extensionFolder = Path.Combine(baseDir, "BrowserExtension");
                string proxyExtensionFolder = Path.Combine(baseDir, "ProxyExtension");

                if (!Directory.Exists(extensionFolder))
                {
                    return BaseResponse<GetEncExtensionContentModelResponse>.Error(
                        $"BrowserExtension folder not found at: {extensionFolder}");
                }

                if (!Directory.Exists(proxyExtensionFolder))
                {
                    return BaseResponse<GetEncExtensionContentModelResponse>.Error(
                        $"ProxyExtension folder not found at: {extensionFolder}");
                }


                //Browser extension files that will help fetching FBM messages.
                string backgroundFilePath = Path.Combine(extensionFolder, "background.js");
                string injectFilePath = Path.Combine(extensionFolder, "inject.js");
                string contentFilePath = Path.Combine(extensionFolder, "content.js");
                string manifestFilePath = Path.Combine(extensionFolder, "manifest.json");
                string signalRFilePath = Path.Combine(extensionFolder, "signalR.min.js");

                //Proxy extension files
                string proxyBackgroundFilePath = Path.Combine(proxyExtensionFolder, "background.js");

                if (!File.Exists(backgroundFilePath))
                {
                    return BaseResponse<GetEncExtensionContentModelResponse>.Error(
                             $"background.js not found at: {backgroundFilePath}");
                }

                if (!File.Exists(proxyBackgroundFilePath))
                {
                    return BaseResponse<GetEncExtensionContentModelResponse>.Error(
                             $"background.js not found at: {proxyBackgroundFilePath}");
                }

                var BackgroundJs = await File.ReadAllTextAsync(backgroundFilePath, cancellationToken);
                var InjectJs = await File.ReadAllTextAsync(injectFilePath, cancellationToken);
                var ContentJs = await File.ReadAllTextAsync(contentFilePath, cancellationToken);
                var ManifestJson = await File.ReadAllTextAsync(manifestFilePath, cancellationToken);
                var SignalRPackage = await File.ReadAllTextAsync(signalRFilePath, cancellationToken);

                var ProxyBackgroundJs = await File.ReadAllTextAsync(proxyBackgroundFilePath, cancellationToken);
                var PrxoyManifestJson = await File.ReadAllTextAsync(manifestFilePath, cancellationToken);

                var settings = await _dbContext.Settings.FirstOrDefaultAsync(cancellationToken);
                var extensionVersion = settings?.Extension_Version;

                // we only update settings if the request is from our portal => UpdateServer will only be true if the request is from our portal
                if (request.UpdateServer)
                {
                    if (settings == null)
                    {
                        var newSettings = new Settings()
                        {
                            Extension_Version = Guid.NewGuid().ToString(),
                            CreatedAt = DateTime.UtcNow
                        };

                        await _dbContext.Settings.AddAsync(newSettings, cancellationToken);

                        extensionVersion = newSettings.Extension_Version;
                    }
                    else
                    {
                        extensionVersion = settings.Extension_Version = Guid.NewGuid().ToString();
                        settings.UpdatedAt = DateTime.UtcNow;
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                var anonymousObj = new
                {
                    BackgroundJs = BackgroundJs,
                    InjectJs = InjectJs,
                    ContentJs = ContentJs,
                    ManifestJson = ManifestJson,
                    SignalRPackage = SignalRPackage,
                    ExtensionVersion = extensionVersion,
                    ProxyBackgroundJs = ProxyBackgroundJs,
                    ProxyManifestJson = PrxoyManifestJson
                };

                string extensionFilesJson = JsonSerializer.Serialize(anonymousObj);

                string encryptedExtensionFiles = _aesEncryptionHelper.Encrypt(extensionFilesJson);

                var response = new GetEncExtensionContentModelResponse()
                {
                    Css = encryptedExtensionFiles
                };

                //Only be true if the reqeust is from our portal.
                if (request.UpdateServer)
                {
                    //Inform all local server that extension file has been changed.

                    await _hubContext.Clients.Group("AllServers")
                       .SendAsync("HandleExtensionFilesChanged", response, cancellationToken);
                }

                return BaseResponse<GetEncExtensionContentModelResponse>.Success("Successfully loaded bootstrap css", response);
            }
            catch (Exception ex)
            {
                return BaseResponse<GetEncExtensionContentModelResponse>.Error("Something went wrong while fetching bootstrap css.");
            }
        }
    }
}
