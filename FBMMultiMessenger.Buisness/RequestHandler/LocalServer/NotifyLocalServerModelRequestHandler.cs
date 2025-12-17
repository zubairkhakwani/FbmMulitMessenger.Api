using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.LocalServer
{
    internal class NotifyLocalServerModelRequestHandler : IRequestHandler<NotifyLocalServerModelRequest, BaseResponse<NotifyLocalServerModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ChatHub _chatHub;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public NotifyLocalServerModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> hubContext, ChatHub chatHub, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext=dbContext;
            _currentUserService=currentUserService;
            _hubContext = hubContext;
            _chatHub = chatHub;
            _webHostEnvironment=webHostEnvironment;
        }
        public async Task<BaseResponse<NotifyLocalServerModelResponse>> Handle(NotifyLocalServerModelRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message) && request.Files is null && request?.Files?.Count == 0)
            {
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Please enter a message or attach a file to send.");
            }

            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currentuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Invalid Request, Please login again to continue");
            }


            var chat = await _dbContext.Chats
                                           .Include(x => x.User)
                                           .ThenInclude(s => s.Subscriptions)
                                           .Include(a => a.Account)
                                           .ThenInclude(ls => ls.LocalServer)
                                           .FirstOrDefaultAsync(c => c.FBChatId == request!.FbChatId
                                           &&
                                           c.UserId == currentUser.Id, cancellationToken);

            if (chat is null)
            {
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Invalid request, Chat does not exist");
            }

            var today = DateTime.UtcNow;
            var activeSubscription = chat.User.Subscriptions
                                                            .Where(x => x.StartedAt <= today
                                                                   &&
                                                                   x.ExpiredAt > today)
                                                            .OrderByDescending(x => x.StartedAt)
                                                            .FirstOrDefault();

            var response = new NotifyLocalServerModelResponse();

            //Extra safety check, a user will have a subscription if he is trying to notifies the extension.
            if (activeSubscription is null)
            {
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Oh Snap, Looks like you dont have any subscription yet", redirectToPackages: true, response);
            }


            var endDate = activeSubscription?.ExpiredAt;

            if (today >= endDate)
            {
                response.IsSubscriptionExpired = true;
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Your subscription has expired. Please renew to continue.", redirectToPackages: true);
            }


            List<string> mediaPaths = new List<string>();

            //Handle Media
            if (request!.Files is not null && request.Files.Count != 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                mediaPaths = HandleMediaFiles(request.Files, wwwRootPath);
            }


            //Notify localserver that user is trying to send message from our app. 
            var sendChatMessage = new NotifyLocalServerDTO()
            {
                IsMessageFromApp = true,
                ChatId = chat.Id,
                FbChatId = request.FbChatId,
                FbAccountId = chat.FbAccountId ?? string.Empty,
                Message = request.Message,
                OfflineUniqueId = request.OfflineUniqueId,
                MediaPaths = mediaPaths
            };


            var hubId = chat.Account.LocalServer.UniqueId;

            await _hubContext.Clients.Group($"{hubId}")
                .SendAsync("HandleChatMessage", sendChatMessage, cancellationToken);


            return BaseResponse<NotifyLocalServerModelResponse>.Success($"Successfully notify extension of the message {request.Message}.", response);
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
