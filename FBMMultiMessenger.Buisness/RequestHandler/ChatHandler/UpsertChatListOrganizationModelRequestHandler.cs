using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class UpsertChatListOrganizationModelRequestHandler
        : IRequestHandler<UpsertChatListOrganizationModelRequest, BaseResponse<UpsertChatListOrganizationModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public UpsertChatListOrganizationModelRequestHandler(
            ApplicationDbContext dbContext,
            CurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<BaseResponse<UpsertChatListOrganizationModelResponse>> Handle(
            UpsertChatListOrganizationModelRequest request,
            CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser is null)
            {
                return BaseResponse<UpsertChatListOrganizationModelResponse>.Error(
                    "Invalid Request, please login again to continue.");
            }

            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == currentUser.Id, cancellationToken);
            if (user is null)
            {
                return BaseResponse<UpsertChatListOrganizationModelResponse>.Error(
                    "Invalid Request, User does not exist.");
            }

            var chat = await _dbContext.Chats
                .FirstOrDefaultAsync(
                    c => c.Id == request.ChatId && c.UserId == currentUser.Id,
                    cancellationToken);
            if (chat is null)
            {
                return BaseResponse<UpsertChatListOrganizationModelResponse>.Error(
                    "Chat not found.");
            }

            var maxPinned = user.MaxPinnedChats > 0 ? user.MaxPinnedChats : 5;
            var maxFavorites = user.MaxFavoriteChats > 0 ? user.MaxFavoriteChats : 50;

            if (request.IsPinned.HasValue)
            {
                if (request.IsPinned.Value && !chat.IsPinned)
                {
                    var pinnedCount = await _dbContext.Chats.CountAsync(
                        c => c.UserId == currentUser.Id && c.IsPinned && c.Id != chat.Id,
                        cancellationToken);
                    if (pinnedCount >= maxPinned)
                    {
                        return BaseResponse<UpsertChatListOrganizationModelResponse>.Error(
                            $"You can pin up to {maxPinned} chats.");
                    }

                    // Newest pin first: shift existing pins down, new pin at 0.
                    var pinned = await _dbContext.Chats
                        .Where(c => c.UserId == currentUser.Id && c.IsPinned && c.Id != chat.Id)
                        .ToListAsync(cancellationToken);
                    foreach (var p in pinned)
                    {
                        p.PinOrder = (p.PinOrder ?? 0) + 1;
                        p.UpdatedAt = DateTime.UtcNow;
                    }

                    chat.IsPinned = true;
                    chat.PinOrder = 0;
                }
                else if (!request.IsPinned.Value && chat.IsPinned)
                {
                    chat.IsPinned = false;
                    chat.PinOrder = null;
                }
            }

            if (request.IsFavorite.HasValue)
            {
                if (request.IsFavorite.Value && !chat.IsFavorite)
                {
                    var favoriteCount = await _dbContext.Chats.CountAsync(
                        c => c.UserId == currentUser.Id && c.IsFavorite && c.Id != chat.Id,
                        cancellationToken);
                    if (favoriteCount >= maxFavorites)
                    {
                        return BaseResponse<UpsertChatListOrganizationModelResponse>.Error(
                            $"You can favorite up to {maxFavorites} chats.");
                    }

                    var favorites = await _dbContext.Chats
                        .Where(c => c.UserId == currentUser.Id && c.IsFavorite && c.Id != chat.Id)
                        .ToListAsync(cancellationToken);
                    foreach (var f in favorites)
                    {
                        f.FavoriteOrder = (f.FavoriteOrder ?? 0) + 1;
                        f.UpdatedAt = DateTime.UtcNow;
                    }

                    chat.IsFavorite = true;
                    chat.FavoriteOrder = 0;
                }
                else if (!request.IsFavorite.Value && chat.IsFavorite)
                {
                    chat.IsFavorite = false;
                    chat.FavoriteOrder = null;
                }
            }

            // Intentionally bump so get-unsynced-messages picks this chat up on other devices.
            chat.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<UpsertChatListOrganizationModelResponse>.Success(
                "Chat list organization updated.",
                new UpsertChatListOrganizationModelResponse
                {
                    ChatId = chat.Id,
                    IsPinned = chat.IsPinned,
                    IsFavorite = chat.IsFavorite,
                    PinOrder = chat.PinOrder,
                    FavoriteOrder = chat.FavoriteOrder,
                });
        }
    }
}
