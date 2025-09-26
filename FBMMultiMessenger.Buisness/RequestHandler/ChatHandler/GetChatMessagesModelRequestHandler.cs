using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Contracts.Response;
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
                                   .Where(cm => cm.ChatId == request.ChatId && cm.Chat.UserId == request.UserId)
                                   .ExecuteUpdateAsync(p => p
                                   .SetProperty(x => x.IsRead, x => true));


            var chatMessages = await _dbContext.ChatMessages
                                                        .Where(cm => cm.ChatId == request.ChatId && cm.Chat.UserId == request.UserId)
                                                        .ToListAsync(cancellationToken);



            var response = chatMessages.Select(x => new GetChatMessagesModelResponse()
            {
                IsReceived = x.IsReceived,
                Message = x.Message,
                IsTextMessage =x.IsTextMessage,
                IsImageMessage = x.IsImageMessage,
                IsVideoMessage = x.IsVideoMessage,
                IsAudioMessage = x.IsAudioMessage,
                CreatedAt = x.CreatedAt

            }).ToList();

            return BaseResponse<List<GetChatMessagesModelResponse>>.Success("Opetation performed successfully", response);
        }
    }
}
