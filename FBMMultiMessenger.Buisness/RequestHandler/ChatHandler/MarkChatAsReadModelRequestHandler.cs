using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
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
    public class MarkChatAsReadModelRequestHandler : IRequestHandler<MarkChatAsReadModelRequest, BaseResponse<MarkChatAsReadModelResponse>>
    {
        private readonly ApplicationDbContext dbContext;
        private readonly CurrentUserService currentUserService;

        public MarkChatAsReadModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this.dbContext = dbContext;
            this.currentUserService = currentUserService;
        }

        public async Task<BaseResponse<MarkChatAsReadModelResponse>> Handle(MarkChatAsReadModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = currentUserService.GetCurrentUser();

            var chat = await dbContext.Chats
                .FirstOrDefaultAsync(c => c.Id == request.ChatId && c.UserId == currentUser.Id);

            if (chat == null)
            {
                return BaseResponse<MarkChatAsReadModelResponse>.Error("Invalid request");
            }

            var messages = await dbContext.ChatMessages.Where(cm => cm.ChatId == chat.Id && !cm.IsRead && cm.Id <= request.LastLocalMessageId)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.UpdatedAt = DateTime.UtcNow;
            }

            if(!chat.IsRead || messages.Any())
            {
                chat.IsRead = true;
                chat.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            var messagesIdsMarkedAsRead = messages.Select(m => m.Id).ToList();

            return BaseResponse<MarkChatAsReadModelResponse>.Success("", new MarkChatAsReadModelResponse()
            {
                MessagesIdsMarkedAsRead = messagesIdsMarkedAsRead,
            });
        }
    }
}
