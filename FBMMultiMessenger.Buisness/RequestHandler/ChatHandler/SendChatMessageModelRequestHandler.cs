using FBMMultiMessenger.Buisness.Request.Chat;
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
using System.Text;
using System.Threading.Tasks;

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
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BaseResponse<SendChatMessageModelResponse>.Error("Please provide message to continue.");
            }


            var chat = await _dbContext.Chats
                                .FirstOrDefaultAsync(x => x.FBChatId == request.FbChatId);
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

            var newChatMessage = new ChatMessages()
            {
                Message = request.Message.Trim(),
                ChatId = chatReference!.Id,
                IsReceived = false,
                IsRead = true,
                IsSent = true,
                IsTextMessage = true,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.ChatMessages.AddAsync(newChatMessage);
            await _dbContext.SaveChangesAsync();

            //Notify the Client that the message has been send. 
            var receivedChat = new ReceiveChatHttpResponse()
            {
                Message = request.Message,
                ChatId = chatReference!.Id,
                FbChatId = chatReference.FBChatId!,
                FbAccountId = chatReference.FbAccountId!,
                FbListingId = chatReference.FbListingId!,
                FbListingTitle = chatReference.FbListingTitle!,
                FbListingLocation = chatReference.FbListingLocation!,
                FbListingPrice = chatReference.FbListingPrice!.Value,
                IsTextMessage = request.IsTextMessage,
                IsVideoMessage =request.IsVideoMessage,
                IsImageMessage = request.IsImageMessage,
                IsAudioMessage = request.IsAudioMessage,
                StartedAt = DateTime.UtcNow,
            };

            await _hubContext.Clients.Group("User123")
                .SendAsync("SendMessage", receivedChat, cancellationToken);

            return BaseResponse<SendChatMessageModelResponse>.Success("Message has been send successfully.", new SendChatMessageModelResponse());
        }
    }
}
