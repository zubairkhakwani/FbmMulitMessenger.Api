using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Data.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FBMMultiMessenger.Buisness.Service
{
    /// <summary>
    /// Loads account products, asks Gemini to match listing title → product, then notifies the
    /// extension to send the reply (with that product's images when requested).
    /// Uses a new DI scope so delayed sends work outside the inbound HTTP context.
    /// </summary>
    public class ChatAutoReplyOrchestrator
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatAutoReplyOrchestrator> _logger;

        public ChatAutoReplyOrchestrator(
            IServiceScopeFactory scopeFactory,
            ILogger<ChatAutoReplyOrchestrator> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>Fire-and-forget entry from HandleChat (does not block the inbound pipeline).</summary>
        public void Enqueue(
            int chatId,
            int accountId,
            int userId,
            string buyerMessage)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var gemini = scope.ServiceProvider.GetRequiredService<GeminiAutoReplyService>();
                    var signalR = scope.ServiceProvider.GetRequiredService<ISignalRService>();

                    await RunAsync(
                        db,
                        gemini,
                        signalR,
                        chatId,
                        accountId,
                        userId,
                        buyerMessage,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI auto-reply failed for chat {ChatId}", chatId);
                }
            });
        }

        private static async Task RunAsync(
            ApplicationDbContext db,
            GeminiAutoReplyService gemini,
            ISignalRService signalR,
            int chatId,
            int accountId,
            int userId,
            string buyerMessage,
            CancellationToken cancellationToken)
        {
            var chat = await db.Chats
                .AsNoTracking()
                .Include(c => c.Account)
                .FirstOrDefaultAsync(
                    c => c.Id == chatId && c.UserId == userId && c.AccountId == accountId,
                    cancellationToken);

            if (chat is null || !chat.IsAutoReplyActive || chat.Account is null)
            {
                return;
            }

            if (!chat.Account.IsExtensionConnected)
            {
                return;
            }

            var today = DateTime.UtcNow;
            var subscription = await db.Subscriptions
                .Where(s =>
                    s.UserId == userId
                    && s.StartedAt <= today
                    && s.ExpiredAt > today)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription is null
                || !subscription.HasAutoReply
                || subscription.AutoReplyLimitUsed >= subscription.AutoReplyMaxLimit)
            {
                return;
            }

            var products = await db.Products
                .AsNoTracking()
                .Include(p => p.Qas)
                .Include(p => p.Images)
                .Where(p =>
                    p.UserId == userId
                    && p.IsActive
                    && p.AccountProducts.Any(ap => ap.AccountId == accountId))
                .ToListAsync(cancellationToken);

            var promptProducts = products
                .Select(p =>
                {
                    var titles = ProductQaHelper.DeserializeQuestions(p.ListingTitles);
                    return new GeminiProductPromptItem
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        ListingTitles = titles,
                        ImageCount = p.Images.Count,
                        Qas = p.Qas
                            .Where(q => q.IsActive)
                            .Select(q => new GeminiProductQaItem
                            {
                                ProductId = p.Id,
                                ProductName = p.Name,
                                QuestionVariantsJson = q.QuestionVariants,
                                Answer = q.Answer,
                            })
                            .ToList(),
                    };
                })
                .Where(p => p.Qas.Count > 0)
                .ToList();

            if (promptProducts.Count == 0)
            {
                return;
            }

            // Oldest → newest, capped. Media is summarized later (no URLs/bytes to Gemini).
            var recentMessages = await db.ChatMessages
                .AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.FBTimestamp ?? long.MinValue)
                .ThenByDescending(m => m.Id)
                .Take(GeminiAutoReplyService.MaxHistoryMessages)
                .ToListAsync(cancellationToken);

            recentMessages.Reverse();

            var correlationId = $"chat-{chatId}-{Guid.NewGuid():N}"[..24];
            var result = await gemini.TryGenerateReplyAsync(
                chat,
                promptProducts,
                buyerMessage,
                recentMessages,
                correlationId,
                cancellationToken);

            if (result is null || string.IsNullOrWhiteSpace(result.Message))
            {
                return;
            }

            var matchedId = result.MatchedProductId;
            if (matchedId is null || promptProducts.All(p => p.ProductId != matchedId))
            {
                matchedId = promptProducts.Count == 1
                    ? promptProducts[0].ProductId
                    : null;
            }

            if (matchedId is null)
            {
                return;
            }

            var mediaPaths = new List<string>();
            if (result.SendProductImages)
            {
                var matched = products.FirstOrDefault(p => p.Id == matchedId);
                if (matched is not null)
                {
                    mediaPaths = matched.Images
                        .OrderBy(i => i.SortOrder)
                        .ThenBy(i => i.Id)
                        .Select(i => i.RelativePath.Replace('\\', '/').TrimStart('/'))
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList()!;
                }
            }

            if (result.DelaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(result.DelaySeconds), cancellationToken);
            }

            // Re-check toggle / extension after delay.
            var still = await db.Chats
                .AsNoTracking()
                .Include(c => c.Account)
                .FirstOrDefaultAsync(
                    c => c.Id == chatId && c.UserId == userId && c.IsAutoReplyActive,
                    cancellationToken);

            if (still?.Account is null || !still.Account.IsExtensionConnected || still.AccountId is null)
            {
                return;
            }

            var payload = new NotifyLocalServer
            {
                IsMessageFromApp = true,
                ChatId = still.Id,
                FbChatId = still.FBChatId,
                FbAccountId = still.FbAccountId ?? string.Empty,
                AccountId = still.AccountId,
                Message = result.Message,
                OfflineUniqueId = ChatMessagesHelper.NewAiOfflineUniqueId(),
                MediaPaths = mediaPaths,
            };

            await signalR.NotifyExtensionMessageSent(payload, still.AccountId.Value, cancellationToken);

            // Count only after a successful send notify (same idea as account LimitUsed++).
            subscription.AutoReplyLimitUsed++;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
