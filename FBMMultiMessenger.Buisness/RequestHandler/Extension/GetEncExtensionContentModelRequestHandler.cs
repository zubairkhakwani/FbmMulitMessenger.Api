using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace FBMMultiMessenger.Buisness.RequestHandler.Extension
{
    internal class GetEncExtensionContentModelRequestHandler : IRequestHandler<GetEncExtensionContentModelRequest, BaseResponse<GetEncExtensionContentModelResponse>>
    {
        private readonly AesEncryptionHelper _aesEncryptionHelper;
        private readonly IHubContext<ChatHub> _hubContext;

        public GetEncExtensionContentModelRequestHandler(AesEncryptionHelper aesEncryptionHelper, IHubContext<ChatHub> hubContext)
        {
            this._aesEncryptionHelper=aesEncryptionHelper;
            this._hubContext=hubContext;
        }
        public async Task<BaseResponse<GetEncExtensionContentModelResponse>> Handle(GetEncExtensionContentModelRequest request, CancellationToken cancellationToken)
        {
            try
            {
                string BaseDir = AppContext.BaseDirectory; // folder of the running app

                string ProjectDir = Path.GetFullPath(Path.Combine(BaseDir, @"..\..\..")); // Go up from bin/Debug/netX.X to project root

                string backgroundFilePath = Path.Combine(ProjectDir, "BrowserExtension", "background.js");
                var BackgroundJs = File.ReadAllText(backgroundFilePath);


                string injectFilePath = Path.Combine(ProjectDir, "BrowserExtension", "inject.js");
                var InjectJs = File.ReadAllText(injectFilePath);


                string contentFilePath = Path.Combine(ProjectDir, "BrowserExtension", "content.js");
                var ContentJs = File.ReadAllText(contentFilePath);


                string maniFestFilePath = Path.Combine(ProjectDir, "BrowserExtension", "manifest.json");
                var ManifestJson = File.ReadAllText(maniFestFilePath);

                string signaRFilePath = Path.Combine(ProjectDir, "BrowserExtension", "signalR.min.js");
                var SignalRPackage = File.ReadAllText(signaRFilePath);


                var anonymousObj = new
                {
                    BackgroundJs = BackgroundJs,
                    InjectJs = InjectJs,
                    ContentJs = ContentJs,
                    ManifestJson = ManifestJson,
                    SignalRPackage = SignalRPackage
                };

                string extensionFilesJson = JsonSerializer.Serialize(anonymousObj);

                string encryptedExtensionFiles = _aesEncryptionHelper.Encrypt(extensionFilesJson);

                var response = new GetEncExtensionContentModelResponse()
                {
                    Css = encryptedExtensionFiles
                };

                if (request.UpdateServer)
                {
                    //Inform our server that extension file has been changed.

                    await _hubContext.Clients.Group("AllServers")
                       .SendAsync("HandleExtensionFilesChanged", response, cancellationToken);
                }

                return BaseResponse<GetEncExtensionContentModelResponse>.Success("Successfully loaded bootstrap css", response);
            }
            catch (Exception)
            {

                return BaseResponse<GetEncExtensionContentModelResponse>.Error("Something went wrong while fetching bootstrap css.");
            }

        }
    }
}
