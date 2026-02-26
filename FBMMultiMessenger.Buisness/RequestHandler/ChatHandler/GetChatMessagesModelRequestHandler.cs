using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

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
                                         .Where(m => m.Id == request.ChatId && m.UserId == currentUser.Id)
                                         .Select(m => m.Id)
                                         .FirstOrDefaultAsync(cancellationToken);

            if (chatId == 0)
            {
                return BaseResponse<List<GetChatMessagesModelResponse>>.Success("Chat not found", new());
            }


            var dbChatMessages = _dbContext.ChatMessages
                                               .AsNoTracking()
                                               .Include(cm => cm.Chat)
                                               .Where(cm => cm.ChatId == chatId)
                                               .OrderBy(x => x.FBTimestamp).ToList();

            var chatMessages = dbChatMessages.Select(x =>
            {
                var replyResult = ChatMessagesHelper.GetMessageReply(new MessageReplyRequest
                {
                    ChatMessages = dbChatMessages,
                    FbMessageReplyId = x.FbMessageReplyId
                });

                var messageReply = replyResult is null ? null : new MessageReplyModelResponse()
                {
                    Type = replyResult.Type,
                    Reply = replyResult.Reply,
                    ReplyTo = replyResult.ReplyTo,
                    Attachments = replyResult.Attachments
                };

                return new GetChatMessagesModelResponse
                {
                    ChatMessageId = x.Id,
                    ChatId = request.ChatId,
                    FbMessageId = x.FbMessageId,
                    FbMessageReplyId = x.FbMessageReplyId,
                    IsReceived = x.IsReceived,
                    Message = x.Message,
                    IsTextMessage = x.IsTextMessage,
                    IsImageMessage = x.IsImageMessage,
                    IsVideoMessage = x.IsVideoMessage,
                    IsAudioMessage = x.IsAudioMessage,
                    IsSent = x.IsSent,
                    MessageReply = messageReply,
                    CreatedAt = x.CreatedAt,
                    FbTimeStamp = x.FBTimestamp
                };
            }).ToList();


            return BaseResponse<List<GetChatMessagesModelResponse>>.Success("Operation performed successfully", chatMessages);
        }
    }
}
