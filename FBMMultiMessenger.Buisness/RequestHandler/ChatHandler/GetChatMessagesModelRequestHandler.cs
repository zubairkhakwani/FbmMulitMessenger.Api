using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class GetChatMessagesModelRequestHandler : IRequestHandler<GetChatMessagesModelRequest, BaseResponse<List<GetChatMessagesModelResponse>>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetChatMessagesModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }

        public async Task<BaseResponse<List<GetChatMessagesModelResponse>>> Handle(GetChatMessagesModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currentuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<List<GetChatMessagesModelResponse>>.Error("Invalid Request, Please login again to continue");
            }

            // Get chat ID first
            var chatId = await _dbContext.Chats
                                         .AsNoTracking()
                                         .Where(m => m.FBChatId == request.FbChatId && m.UserId == currentUser.Id)
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

            var chatMessages = await _dbContext.ChatMessages
                .AsNoTracking()
                .Where(cm => cm.ChatId == chatId)
                .OrderBy(x => x.Id)
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
