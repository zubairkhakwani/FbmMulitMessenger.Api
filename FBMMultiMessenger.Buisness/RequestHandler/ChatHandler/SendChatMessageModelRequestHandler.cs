using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class SendChatMessageModelRequestHandler : IRequestHandler<SendChatMessageModelRequest, BaseResponse<SendChatMessageModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public SendChatMessageModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<BaseResponse<SendChatMessageModelResponse>> Handle(SendChatMessageModelRequest request, CancellationToken cancellationToken)
        {
            var chat = await _dbContext.Chats.FirstOrDefaultAsync(c => c.Id == request.ChatId && c.UserId == request.UserId);

            if (chat is null)
            {
                return BaseResponse<SendChatMessageModelResponse>.Error("Invalid request, Chat does not exist");
            }
            var newChatMessage = new ChatMessages()
            {
                Message = request.Message.Trim(),
                ChatId = request.ChatId,
                IsReceived = false,
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.ChatMessages.AddAsync(newChatMessage);
            await _dbContext.SaveChangesAsync();

            return BaseResponse<SendChatMessageModelResponse>.Success("Chat has been send", new SendChatMessageModelResponse());
        }
    }
}
