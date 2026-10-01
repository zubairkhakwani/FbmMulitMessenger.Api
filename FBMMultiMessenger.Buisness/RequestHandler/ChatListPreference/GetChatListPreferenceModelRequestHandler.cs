using FBMMultiMessenger.Buisness.Request.ChatListPreference;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatListPreference
{
    internal class GetChatListPreferenceModelRequestHandler
        : IRequestHandler<GetChatListPreferenceModelRequest, BaseResponse<GetChatListPreferenceModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetChatListPreferenceModelRequestHandler(
            ApplicationDbContext dbContext,
            CurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<BaseResponse<GetChatListPreferenceModelResponse>> Handle(
            GetChatListPreferenceModelRequest request,
            CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser is null)
            {
                return BaseResponse<GetChatListPreferenceModelResponse>.Error(
                    "Invalid Request, please login again to continue.");
            }

            var rows = await _dbContext.UserChatListPreferences
                .AsNoTracking()
                .Where(x => x.UserId == currentUser.Id && (x.IsPinned || x.IsFavorite))
                .ToListAsync(cancellationToken);

            var response = new GetChatListPreferenceModelResponse
            {
                PinnedChatIds = rows
                    .Where(x => x.IsPinned)
                    .OrderBy(x => x.PinOrder ?? int.MaxValue)
                    .ThenBy(x => x.Id)
                    .Select(x => x.ChatId)
                    .ToList(),
                FavoriteChatIds = rows
                    .Where(x => x.IsFavorite)
                    .OrderBy(x => x.FavoriteOrder ?? int.MaxValue)
                    .ThenBy(x => x.Id)
                    .Select(x => x.ChatId)
                    .ToList(),
            };

            return BaseResponse<GetChatListPreferenceModelResponse>.Success(
                "Operation performed successfully.",
                response);
        }
    }
}
