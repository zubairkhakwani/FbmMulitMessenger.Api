using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class SetChatAutoReplyActiveModelRequestHandler : IRequestHandler<SetChatAutoReplyActiveModelRequest, BaseResponse<SetChatAutoReplyActiveModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IUserAccountService _userAccountService;

        public SetChatAutoReplyActiveModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IUserAccountService userAccountService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userAccountService = userAccountService;
        }

        public async Task<BaseResponse<SetChatAutoReplyActiveModelResponse>> Handle(SetChatAutoReplyActiveModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return BaseResponse<SetChatAutoReplyActiveModelResponse>.Error("Invalid request, Please login again to continue");
            }

            var chat = await _dbContext.Chats
                                       .FirstOrDefaultAsync(
                                         c => c.Id == request.ChatId && c.UserId == currentUser.Id,
                                         cancellationToken);

            if (chat is null)
            {
                return BaseResponse<SetChatAutoReplyActiveModelResponse>.Error("Chat does not exist");
            }

            if (request.IsAutoReplyActive)
            {
                var subscriptions = await _dbContext.Subscriptions
                                                    .Where(s => s.UserId == currentUser.Id)
                                                    .ToListAsync(cancellationToken);

                var activeSubscription = _userAccountService.GetActiveSubscription(subscriptions);

                if (activeSubscription is null)
                {
                    return BaseResponse<SetChatAutoReplyActiveModelResponse>.Error("Yor subscription is either expired or you have not applied yet.");
                }

                if (!activeSubscription.HasAutoReply)
                {
                    return BaseResponse<SetChatAutoReplyActiveModelResponse>.Error("AI Auto-reply is not included in your plan.", redirectToPackages: true);
                }
            }

            chat.IsAutoReplyActive = request.IsAutoReplyActive;

            chat.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var responseMessage = request.IsAutoReplyActive
                                         ? "Auto-reply enabled for this chat"
                                         : "Auto-reply disabled for this chat";

            return BaseResponse<SetChatAutoReplyActiveModelResponse>.Success(responseMessage,
                new SetChatAutoReplyActiveModelResponse
                {
                    ChatId = chat.Id,
                    IsAutoReplyActive = chat.IsAutoReplyActive,
                });
        }
    }
}
