using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Extension
{
    internal class NotifyExtensionModelRequestHandler : IRequestHandler<NotifyExtensionModelRequest, BaseResponse<NotifyExtensionModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ChatHub _chatHub;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public NotifyExtensionModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> hubContext, ChatHub chatHub, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext=dbContext;
            this._currentUserService=currentUserService;
            _hubContext = hubContext;
            _chatHub = chatHub;
            this._webHostEnvironment=webHostEnvironment;
        }
        public async Task<BaseResponse<NotifyExtensionModelResponse>> Handle(NotifyExtensionModelRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message) && request.Files is null && request?.Files?.Count == 0)
            {
                return BaseResponse<NotifyExtensionModelResponse>.Error("Please enter a message or attach a file to send.");
            }

            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currentuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<NotifyExtensionModelResponse>.Error("Invalid Request, Please login again to continue");
            }


            var chat = await _dbContext.Chats
                                           .Include(x => x.User)
                                           .ThenInclude(s => s.Subscriptions)
                                           .FirstOrDefaultAsync(c => c.FBChatId == request!.FbChatId
                                           &&
                                           c.UserId == currentUser.Id, cancellationToken);

            if (chat is null)
            {
                return BaseResponse<NotifyExtensionModelResponse>.Error("Invalid request, Chat does not exist");
            }

            var today = DateTime.UtcNow;
            var activeSubscription = chat.User.Subscriptions
                                                            .Where(x => x.StartedAt <= today
                                                                   &&
                                                                   x.ExpiredAt > today)
                                                            .OrderByDescending(x => x.StartedAt)
                                                            .FirstOrDefault();

            var response = new NotifyExtensionModelResponse();

            //Extra safety check, a user will have a subscription if he is trying to notifies the extension.
            if (activeSubscription is null)
            {
                return BaseResponse<NotifyExtensionModelResponse>.Error("Oh Snap, Looks like you dont have any subscription yet", redirectToPackages: true, response);
            }


            var endDate = activeSubscription?.ExpiredAt;

            if (today >= endDate)
            {
                response.IsSubscriptionExpired = true;
                return BaseResponse<NotifyExtensionModelResponse>.Error("Your subscription has expired. Please renew to continue.", redirectToPackages: true);
            }


            List<string> mediaPaths = new List<string>();

            //Handle Media
            if (request!.Files is not null && request.Files.Count != 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                mediaPaths = HandleMediaFiles(request.Files, wwwRootPath);
            }


            //Notify extension that user is trying to send message from our app. 
            var sendChatMessage = new NotifyExtensionDTO()
            {
                IsMessageFromApp = true,
                ChatId = chat.Id,
                FbChatId = request.FbChatId,
                Message = request.Message,
                OfflineUniqueId = request.OfflineUniqueId,
                MediaPaths = mediaPaths
            };

            await _hubContext.Clients.Group($"Extension_{chat.FbAccountId}")
                .SendAsync("SendMessage", sendChatMessage, cancellationToken);


            return BaseResponse<NotifyExtensionModelResponse>.Success($"Successfully notify extension of the message {request.Message}.", response);
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

                string relativeUrl = $"ChatMessages/Media/{fileName}";
                mediaPaths.Add(relativeUrl);
            }
            return mediaPaths;
        }
    }
}
