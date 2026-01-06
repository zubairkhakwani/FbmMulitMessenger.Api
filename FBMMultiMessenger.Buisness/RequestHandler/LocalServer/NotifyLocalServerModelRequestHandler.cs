using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.LocalServer
{
    internal class NotifyLocalServerModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, IUserAccountService _userAccountService, ISignalRService _signalRService, IWebHostEnvironment _webHostEnvironment) : IRequestHandler<NotifyLocalServerModelRequest, BaseResponse<NotifyLocalServerModelResponse>>
    {
        public async Task<BaseResponse<NotifyLocalServerModelResponse>> Handle(NotifyLocalServerModelRequest request, CancellationToken cancellationToken)
        {
            var errorResponse = new NotifyLocalServerModelResponse()
            {
                OfflineUniqueId = request.OfflineUniqueId
            };

            if (string.IsNullOrWhiteSpace(request.Message) && request.Files is null && request?.Files?.Count == 0)
            {
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Please enter a message or attach a file to send.", result: errorResponse);
            }

            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currentuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Invalid Request, Please login again to continue", result: errorResponse);
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
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Invalid request, Chat does not exist", result: errorResponse);
            }

            var userSubscriptions = chat.User.Subscriptions;
            var activeSubscription = _userAccountService.GetActiveSubscription(userSubscriptions) ?? _userAccountService.GetLastActiveSubscription(userSubscriptions);

            //Extra safety check, a user will have a subscription if he is trying to notifies the extension.
            if (activeSubscription is null)
            {
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Oh Snap, Looks like you dont have any subscription yet", redirectToPackages: true, result: errorResponse);
            }

            var isSubscriptionExpired = _userAccountService.IsSubscriptionExpired(activeSubscription);

            if (isSubscriptionExpired)
            {
                errorResponse.IsSubscriptionExpired = true;
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Your subscription has expired. Please renew to continue.", redirectToPackages: true, result: errorResponse);
            }

            List<string> mediaPaths = new List<string>();

            //Handle Media
            if (request!.Files is not null && request.Files.Count != 0)
            {
                mediaPaths = HandleMediaFiles(request.Files);
            }

            var accountLocalServer = chat.Account?.LocalServer;

            //if local server is null or offline return error
            if (accountLocalServer is null || !accountLocalServer.IsOnline)
            {
                return BaseResponse<NotifyLocalServerModelResponse>.Error("Failed to send message", result: errorResponse);
            }

            //Notify localserver that user is trying to send message from our app. 
            var sendChatMessage = new NotifyLocalServer()
            {
                IsMessageFromApp = true,
                ChatId = chat.Id,
                FbChatId = request.FbChatId,
                FbAccountId = chat.FbAccountId ?? string.Empty,
                AccountId = chat.AccountId,
                Message = request.Message,
                OfflineUniqueId = request.OfflineUniqueId,
                MediaPaths = mediaPaths
            };

            await _signalRService.NotifyLocalServerMessageSent(sendChatMessage, accountLocalServer.UniqueId, cancellationToken);

            return BaseResponse<NotifyLocalServerModelResponse>.Success($"Successfully notify extension of the message {request.Message}.", new NotifyLocalServerModelResponse());
        }

        public List<string> HandleMediaFiles(List<IFormFile> files)
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            List<string> mediaPaths = new List<string>();

            foreach (var file in files)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string filePath = @"Images\ChatMessages";
                string finalPath = Path.Combine(wwwRootPath, filePath);

                if (!Directory.Exists(finalPath))
                {
                    Directory.CreateDirectory(finalPath);
                }

                using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                string relativeUrl = $"Images/ChatMessages/{fileName}";
                mediaPaths.Add(relativeUrl);
            }
            return mediaPaths;
        }
    }
}
