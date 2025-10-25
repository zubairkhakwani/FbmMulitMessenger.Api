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
using System.Text.Json;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class GetChatMessagesModelRequestHandler : IRequestHandler<GetChatMessagesModelRequest, BaseResponse<List<GetChatMessagesModelResponse>>>
    {
        private readonly ApplicationDbContext _dbContext;

        public GetChatMessagesModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }

        public async Task<BaseResponse<List<GetChatMessagesModelResponse>>> Handle(GetChatMessagesModelRequest request, CancellationToken cancellationToken)
        {
            // Get chat ID first
            var chatId = await _dbContext.Chats
                .Where(m => m.FBChatId == request.FbChatId && m.UserId == request.UserId)
                .Select(m => m.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (chatId == 0)
            {
                return BaseResponse<List<GetChatMessagesModelResponse>>.Success("Chat not found", new());
            }

            // Bulk update (fast, no loading into memory)
            await _dbContext.Chats
                .Where(c => c.Id == chatId)
                .ExecuteUpdateAsync(p => p.SetProperty(m => m.IsRead, true), cancellationToken);

            await _dbContext.ChatMessages
                .Where(m => m.ChatId == chatId)
                .ExecuteUpdateAsync(p => p.SetProperty(m => m.IsRead, true), cancellationToken);

            // Load messages for response (with AsNoTracking for performance)
            var chatMessages = await _dbContext.ChatMessages
                .AsNoTracking()
                .Where(cm => cm.ChatId == chatId)
                .Select(x => new GetChatMessagesModelResponse()
                {
                    FbChatId = request.FbChatId,
                    IsReceived = x.IsReceived,
                    Message = x.Message,
                    IsTextMessage = x.IsTextMessage,
                    IsImageMessage = x.IsImageMessage,
                    IsVideoMessage = x.IsVideoMessage,
                    IsAudioMessage = x.IsAudioMessage,
                    IsSent = x.IsSent,
                    CreatedAt = x.CreatedAt,
                })
                .ToListAsync(cancellationToken);


            return BaseResponse<List<GetChatMessagesModelResponse>>.Success("Operation performed successfully", chatMessages);
        }
    }
}
