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
            await _dbContext.ChatMessages
                                .Where(m => m.Chat.FBChatId == request.FbChatId
                                        &&
                                       m.Chat.UserId == request.UserId)
                                .ExecuteUpdateAsync(p => p
                                .SetProperty(m => m.IsRead, m => true));


            var chatMessages = await _dbContext.ChatMessages
                                                        .Include(c=>c.Chat)
                                                        .Where(cm => cm.Chat.FBChatId == request.FbChatId
                                                              &&
                                                              cm.Chat.UserId == request.UserId)
                                                        .ToListAsync(cancellationToken);

            var response = chatMessages.Select(x => new GetChatMessagesModelResponse()
            {
                FbChatId = x.Chat.FBChatId,
                IsReceived = x.IsReceived,
                Message = x.IsTextMessage ? x.Message : "",
                IsTextMessage = x.IsTextMessage,
                IsImageMessage = x.IsImageMessage,
                IsVideoMessage = x.IsVideoMessage,
                IsAudioMessage = x.IsAudioMessage,
                IsSent = x.IsSent,
                CreatedAt = x.CreatedAt,
                FileData = GetImages(x)

            }).ToList();


            return BaseResponse<List<GetChatMessagesModelResponse>>.Success("Opetation performed successfully", response);
        }

        private List<FileDataModelResponse> GetImages(ChatMessages message)
        {
            if(!message.IsImageMessage)
            {
                return new List<FileDataModelResponse>();
            }

            var imagesList = JsonSerializer.Deserialize<List<string>>(message.Message);

            var fileModel = imagesList.Select(i => new FileDataModelResponse()
            {
                FileUrl = i

            }).ToList();

            return fileModel;
        }
    }
}
