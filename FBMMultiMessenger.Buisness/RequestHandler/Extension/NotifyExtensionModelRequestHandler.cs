using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.Extension
{
    internal class NotifyExtensionModelRequestHandler : IRequestHandler<NotifyExtensionModelRequest, BaseResponse<NotifyExtensionModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private string baseUrl;

        public NotifyExtensionModelRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _dbContext=dbContext;
            _hubContext = hubContext;
            this._webHostEnvironment=webHostEnvironment;
            baseUrl  = configuration.GetValue<string>("Urls:BaseUrl")!;
        }
        public async Task<BaseResponse<NotifyExtensionModelResponse>> Handle(NotifyExtensionModelRequest request, CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(request.Message) && request.Files is null && request?.Files?.Count == 0)
            {
                return BaseResponse<NotifyExtensionModelResponse>.Error("Please enter a message or attach a file to send.");
            }

            var chat = await _dbContext.Chats
                                           .Include(x => x.User)
                                           .ThenInclude(s => s.Subscription)
                                           .FirstOrDefaultAsync(c => c.FBChatId == request!.FbChatId
                                           &&
                                           c.UserId == request.UserId, cancellationToken);


            if (chat is null)
            {
                return BaseResponse<NotifyExtensionModelResponse>.Error("Invalid request, Chat does not exist");
            }

            var subscription = chat.User.Subscription;

            //Extra safety check, a user will have a subscription if he is trying to notifies the extension.
            if (subscription is null)
            {
                return BaseResponse<NotifyExtensionModelResponse>.Error("Oh Snap, Looks like you dont have any subscription yet", redirectToPackages: true);
            }

            var today = DateTime.Now;
            var endDate = subscription.ExpiredAt;

            if (today >= endDate)
            {
                return BaseResponse<NotifyExtensionModelResponse>.Error("Your subscription has expired. Please renew to continue.", redirectToPackages: true);
            }


            List<string> mediaPaths = new List<string>();

            //Handle Media
            if (request!.Files is not null)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                mediaPaths = HandleMediaFiles(request.Files, wwwRootPath);
            }


            //Notify extension that a user is trying to send a message
            var sendChatMessage = new NotifyExtensionDTO()
            {
                IsMessageFromApp = true,
                ChatId = chat.Id,
                FbChatId = request.FbChatId,
                Message = request.Message,
                MediaPaths = mediaPaths
            };

            await _hubContext.Clients.All.SendAsync("SendMessage", sendChatMessage, cancellationToken);

            return BaseResponse<NotifyExtensionModelResponse>.Success($"Successfully notify extension of the message {request.Message}.", new NotifyExtensionModelResponse());
        }

        public List<string> HandleMediaFiles(List<IFormFile> files, string wwwRootPath)
        {
            List<string> mediaPaths = new List<string>();

            foreach (var file in files)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string filePath = @"ChatMessages\Media";
                string finalPath = Path.Combine(wwwRootPath, filePath);

                if (!Directory.Exists(finalPath))
                {
                    Directory.CreateDirectory(finalPath);
                }

                using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                string relativeUrl = $"{baseUrl}/ChatMessages/Media/{fileName}";
                mediaPaths.Add(relativeUrl);
            }
            return mediaPaths;
        }
    }
}
