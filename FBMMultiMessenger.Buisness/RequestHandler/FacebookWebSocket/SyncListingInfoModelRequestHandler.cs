using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.FacebookWebSocket;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Data.DB;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.FacebookWebSocket
{
    public class SyncListingInfoModelRequestHandler : IRequestHandler<SyncListingInfoModelRequest>
    {
        private readonly ApplicationDbContext dbContext;
        private readonly CurrentUserService currentUserService;
        private readonly ISignalRService signalRService;

        public SyncListingInfoModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, ISignalRService signalRService)
        {
            this.dbContext = dbContext;
            this.currentUserService = currentUserService;
            this.signalRService = signalRService;
        }

        public async Task Handle(SyncListingInfoModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = currentUserService.GetCurrentUser();

            var chat = dbContext.Chats.FirstOrDefault(c => c.Id == request.ChatId && c.UserId == currentUser.Id);

            if(chat == null)
            {
                return;
            }

            var anyChangesMade = false;

            if(string.IsNullOrWhiteSpace(chat.FbListingTitle) && !string.IsNullOrWhiteSpace(request.FbListingTitle))
            {
                chat.FbListingTitle = request.FbListingTitle;
                anyChangesMade = true;
            }

            if (string.IsNullOrWhiteSpace(chat.FBListingImage) && !string.IsNullOrWhiteSpace(request.FbListingImg))
            {
                chat.FBListingImage = request.FbListingImg;
                anyChangesMade = true;
            }

            if (string.IsNullOrWhiteSpace(chat.FbListingId) && !string.IsNullOrWhiteSpace(request.FbListingId))
            {
                chat.FbListingId = request.FbListingId;
                anyChangesMade = true;
            }

            if (string.IsNullOrWhiteSpace(chat.UserProfileImage) && !string.IsNullOrWhiteSpace(request.UserProfileImg))
            {
                chat.UserProfileImage = request.UserProfileImg;
                anyChangesMade = true;
            }

            if(anyChangesMade)
            {
                var signalRRequest = new ChatInfoUpdatedSignalRModel
                {
                    ChatId = chat.Id,
                    FbListingId = chat.FbListingId,
                    FbListingTitle = chat.FbListingTitle,
                    FbListingImage = chat.FBListingImage,
                    FbListingLocation = chat.FbListingLocation,
                    FbListingPrice = chat.FbListingPrice,
                    OtherUserId = chat.OtherUserId,
                    OtherUserName = chat.OtherUserName,
                    OtherUserProfilePicture = chat.UserProfileImage
                };

                await signalRService.NotifyAppChatInfoUpdated(chat.UserId, signalRRequest, cancellationToken);


                chat.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
