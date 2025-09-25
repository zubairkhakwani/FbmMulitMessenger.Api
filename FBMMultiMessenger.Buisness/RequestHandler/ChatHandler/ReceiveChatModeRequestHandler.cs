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
            var chatId = chat?.Id;
            if (chat is null)
            {
                var newChat = new Chat()
                {
                    AccountId = request.AccountId,
                    UserId = request.UserId,

                    FbUserId = request.FbUserId,
                    FBChatId = request.FbChatId,
                    FbAccountId = request.FbAccountId,
                    FbListingId = request.FbListingId,
                    FbListingTitle = request.FbListingTitle,
                    FbListingLocation = request.FbListingLocation,
                    FbListingPrice = request.FbListingPrice,

                    ImagePath = null,
                    IsRead = false,
                    StartedAt = DateTime.Now,
                };

                await _dbContext.AddAsync(newChat, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                chatId = newChat.Id;
            }

            var newChatMessage = new ChatMessages()
            {
                Message = request.Message,
                ChatId = chatId!.Value,
                IsReceived = request.IsReceived,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.ChatMessages.AddAsync(newChatMessage, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (request.IsReceived)
            {
                var receivedChat = new ReceiveChatHttpResponse()
                {
                    Message = request.Message,
                    ChatId = chatId.Value,
                    FbUserId = request.FbUserId,
                    FbChatId = request.FbChatId,
                    FbAccountId = request.FbAccountId,
                    FbListingId = request.FbListingId,
                    FbListingTitle = request.FbListingTitle,
                    FbListingLocation = request.FbListingLocation,
                    FbListingPrice = request.FbListingPrice,
                    StartedAt = DateTime.Now,
                };
                await _hubContext.Clients.Group("User123")
                    .SendAsync("ReceiveMessage", receivedChat, cancellationToken);
            }


            var acionPerformed = request.IsReceived ? "received" : "send";

            return BaseResponse<ReceiveChatModelResponse>.Success($"Message has been {acionPerformed} successfully", new ReceiveChatModelResponse());
        }
    }
}