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
using System.Text.Json;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class ReceiveChatModeRequestHandler : IRequestHandler<ReceiveChatModelRequest, BaseResponse<ReceiveChatModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;

        public ReceiveChatModeRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext)
        {
            this._dbContext=dbContext;
            this._hubContext = hubContext;
        }
        public async Task<BaseResponse<ReceiveChatModelResponse>> Handle(ReceiveChatModelRequest request, CancellationToken cancellationToken)
        {
            var chat = await _dbContext.Chats.FirstOrDefaultAsync(x => x.FBChatId == request.FbChatId);
            var chatReference = chat;
            if (chat is null)
            {
                var newChat = new Chat()
                {
                    //AccountId = request.AccountId,
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
                IsReceived = true,
                IsRead = false,
                IsTextMessage = request.IsTextMessage,
                IsVideoMessage = request.IsVideoMessage,
                IsImageMessage = request.IsImageMessage,
                IsAudioMessage = request.IsAudioMessage,

                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.ChatMessages.AddAsync(newChatMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);


            //Inform the client that the message has been received.
            var receivedChat = new ReceiveChatHttpResponse()
            {
                Message = dbMessage,
                ChatId = chatReference.Id,
                FbChatId = request.FbChatId,
                FbAccountId = request.FbAccountId,
                FbListingId = request.FbListingId,
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
                .SendAsync("ReceiveMessage", receivedChat, cancellationToken);


            return BaseResponse<ReceiveChatModelResponse>.Success($"Message has been received successfully", new ReceiveChatModelResponse());
        }
    }
}