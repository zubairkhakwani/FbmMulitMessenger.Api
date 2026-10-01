using FBMMultiMessenger.Buisness.Request.ChatListPreference;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatListPreference
{
    internal class UpsertChatListPreferenceModelRequestHandler
        : IRequestHandler<UpsertChatListPreferenceModelRequest, BaseResponse<GetChatListPreferenceModelResponse>>
    {
        public const int MaxPinnedChats = 5;
        public const int MaxFavoriteChats = 50;

        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public UpsertChatListPreferenceModelRequestHandler(
            ApplicationDbContext dbContext,
            CurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<BaseResponse<GetChatListPreferenceModelResponse>> Handle(
            UpsertChatListPreferenceModelRequest request,
            CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser is null)
            {
                return BaseResponse<GetChatListPreferenceModelResponse>.Error(
                    "Invalid Request, please login again to continue.");
            }

            var pinnedIds = DeduplicatePreserveOrder(request.PinnedChatIds ?? new List<int>());
            var favoriteIds = DeduplicatePreserveOrder(request.FavoriteChatIds ?? new List<int>());

            if (pinnedIds.Count > MaxPinnedChats)
            {
                return BaseResponse<GetChatListPreferenceModelResponse>.Error(
                    $"You can pin up to {MaxPinnedChats} chats.");
            }

            if (favoriteIds.Count > MaxFavoriteChats)
            {
                return BaseResponse<GetChatListPreferenceModelResponse>.Error(
                    $"You can favorite up to {MaxFavoriteChats} chats.");
            }

            var allIds = pinnedIds.Union(favoriteIds).ToList();
            if (allIds.Count > 0)
            {
                var ownedChatIds = await _dbContext.Chats
                    .AsNoTracking()
                    .Where(c => c.UserId == currentUser.Id && allIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);

                var ownedSet = ownedChatIds.ToHashSet();
                pinnedIds = pinnedIds.Where(ownedSet.Contains).ToList();
                favoriteIds = favoriteIds.Where(ownedSet.Contains).ToList();
            }

            var existing = await _dbContext.UserChatListPreferences
                .Where(x => x.UserId == currentUser.Id)
                .ToListAsync(cancellationToken);

            _dbContext.UserChatListPreferences.RemoveRange(existing);

            var now = DateTime.UtcNow;
            var pinnedSet = pinnedIds.ToHashSet();
            var favoriteSet = favoriteIds.ToHashSet();
            var chatIdsToWrite = pinnedSet.Union(favoriteSet).ToList();

            foreach (var chatId in chatIdsToWrite)
            {
                var isPinned = pinnedSet.Contains(chatId);
                var isFavorite = favoriteSet.Contains(chatId);
                _dbContext.UserChatListPreferences.Add(new UserChatListPreference
                {
                    UserId = currentUser.Id,
                    ChatId = chatId,
                    IsPinned = isPinned,
                    IsFavorite = isFavorite,
                    PinOrder = isPinned ? pinnedIds.IndexOf(chatId) : null,
                    FavoriteOrder = isFavorite ? favoriteIds.IndexOf(chatId) : null,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = new GetChatListPreferenceModelResponse
            {
                PinnedChatIds = pinnedIds,
                FavoriteChatIds = favoriteIds,
            };

            return BaseResponse<GetChatListPreferenceModelResponse>.Success(
                "Chat list preferences saved.",
                response);
        }

        private static List<int> DeduplicatePreserveOrder(IEnumerable<int> ids)
        {
            var seen = new HashSet<int>();
            var result = new List<int>();
            foreach (var id in ids)
            {
                if (id <= 0 || !seen.Add(id)) continue;
                result.Add(id);
            }
            return result;
        }
    }
}
