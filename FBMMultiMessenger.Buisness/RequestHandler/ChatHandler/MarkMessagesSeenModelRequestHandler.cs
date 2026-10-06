using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.ChatHandler
{
    internal class MarkMessagesSeenModelRequestHandler : IRequestHandler<MarkMessagesSeenModelRequest>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISignalRService _signalRService;

        public MarkMessagesSeenModelRequestHandler(ApplicationDbContext dbContext, ISignalRService signalRService)
        {
            _dbContext = dbContext;
            _signalRService = signalRService;
        }

        public async Task Handle(MarkMessagesSeenModelRequest request, CancellationToken cancellationToken)
        {
            // Only the OTHER participant reading our messages counts as "seen". When the reader is the
            // account itself, it's us reading their messages — not relevant for the seen feature.
            if (string.IsNullOrEmpty(request.ReaderUserId) ||
                string.Equals(request.ReaderUserId, request.FbAccountId, StringComparison.Ordinal))
            {
                return;
            }

            var chat = await _dbContext.Chats
                .Where(c => c.FBChatId == request.FbChatId && c.AccountId == request.AccountId && c.UserId == request.CurrentUserId)
                .Select(c => new { c.Id, c.UserId })
                .FirstOrDefaultAsync(cancellationToken);

            if (chat is null)
            {
                return;
            }

            var seenAt = DateTimeOffset.FromUnixTimeMilliseconds(request.ReadActionMs).UtcDateTime;
            var now = DateTime.UtcNow;

            // Mark our sent, not-yet-seen messages up to the watermark as seen (single UPDATE statement).
            var updated = await _dbContext.ChatMessages
                .Where(m => m.ChatId == chat.Id
                            && m.IsSent && !m.IsReceived
                            && m.SeenAt == null
                            && m.FBTimestamp != null && m.FBTimestamp <= request.WatermarkMs)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.SeenAt, seenAt)
                    .SetProperty(m => m.UpdatedAt, now), cancellationToken);

            if (updated == 0)
            {
                return;
            }

            // Bump the chat so get-unsynced-messages returns the updated messages (other devices / resync).
            await _dbContext.Chats
                .Where(c => c.Id == chat.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UpdatedAt, now), cancellationToken);

            // Live update so the app flips the UI to "Seen" immediately.
            await _signalRService.NotifyAppMessagesSeen(chat.UserId, new ChatMessagesSeenSignalRModel
            {
                ChatId = chat.Id,
                SeenWatermarkMs = request.WatermarkMs,
                SeenAt = seenAt
            }, cancellationToken);
        }
    }
}
