using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    public class SendChatMessageModelRequestHandler : IRequestHandler<SendChatMessageModelRequest, BaseResponse<SendChatMessageModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;

        public SendChatMessageModelRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext)
        {
            this._dbContext=dbContext;
            this._hubContext = hubContext;
        }
        public async Task<BaseResponse<SendChatMessageModelResponse>> Handle(SendChatMessageModelRequest request, CancellationToken cancellationToken)
        {
            if (request.Messages.Count == 0)
            {
                return BaseResponse<SendChatMessageModelResponse>.Error("Please provide message to continue.");
            }

            var chat = await _dbContext.Chats
                                       .Include(u => u.User)
                                       .ThenInclude(s => s.Subscription)
                                       .FirstOrDefaultAsync(x => x.FBChatId == request.FbChatId, cancellationToken);

            var chatReference = chat;

            if (chat is null)
            {
                var newChat = new Chat()
                {
                    //AccountId = request
                    UserId = request.UserId,

                    FBChatId = request.FbChatId,
                    FbAccountId = request.FbAccountId,
                    FbListingId = request.FbListingId,
                    FbListingTitle = string.IsNullOrWhiteSpace(request.FbListingTitle) ? "No title" : request.FbListingTitle,
                    FbListingLocation = string.IsNullOrWhiteSpace(request.FbListingLocation) ? "No location" : request.FbListingLocation,
                    FbListingPrice = request.FbListingPrice,

                    ImagePath = null,
                    IsRead = false,
                    StartedAt = DateTime.Now,

                };

                await _dbContext.AddAsync(newChat, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                chatReference = newChat;
            }

            var dbMessage = string.Empty;

            if (request.IsImageMessage)
            {
                dbMessage = JsonSerializer.Serialize(request.Messages);
            }
            else if (request.IsTextMessage)
            {
                dbMessage = request.Messages.FirstOrDefault()?.Trim();
            }

            var newChatMessage = new ChatMessages()
            {
                Message = dbMessage.Trim(),
                ChatId = chatReference!.Id,
                IsReceived = false,
                IsRead = true,
                IsSent = true,
                IsTextMessage = request.IsTextMessage,
                IsAudioMessage = request.IsAudioMessage,
                IsImageMessage = request.IsImageMessage,
                IsVideoMessage = request.IsVideoMessage,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.ChatMessages.AddAsync(newChatMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var user = chatReference.User;
            var isExpired = user.Subscription.IsExpired;

            if (!isExpired)
            {
                //Notify the Client only if subscription is active.
                var receivedChat = new ReceiveChatHttpResponse()
                {
                    Message = request.Messages.FirstOrDefault()!,
                    ChatId = chatReference!.Id,
                    FbChatId = chatReference.FBChatId!,
                    FbAccountId = chatReference.FbAccountId!,
                    FbListingId = chatReference.FbListingId!,
                    FbListingTitle = chatReference.FbListingTitle!,
                    FbListingLocation = chatReference.FbListingLocation!,
                    FbListingPrice = chatReference.FbListingPrice!.Value,
                    IsTextMessage = request.IsTextMessage,
                    IsVideoMessage = request.IsVideoMessage,
                    IsImageMessage = request.IsImageMessage,
                    IsAudioMessage = request.IsAudioMessage,
                    StartedAt = DateTime.UtcNow,
                };

                await _hubContext.Clients.Group("User123")
                    .SendAsync("SendMessage", receivedChat, cancellationToken);
            }

            return BaseResponse<SendChatMessageModelResponse>.Success("Message has been send successfully.", new SendChatMessageModelResponse());
        }
    }
}
