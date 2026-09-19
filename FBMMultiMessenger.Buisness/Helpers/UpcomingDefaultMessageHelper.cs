using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.Helpers
{
    /// <summary>
    /// Upcoming DMs are not linked to accounts. Specific DefaultMessageId wins;
    /// otherwise use the user's ApplyToUpcomingAccounts message text.
    /// </summary>
    internal static class UpcomingDefaultMessageHelper
    {
        public static async Task ClearOtherUpcomingFlagsAsync(ApplicationDbContext db, int userId, int keepDefaultMessageId, CancellationToken cancellationToken)
        {
            var others = await db.DefaultMessages
                                 .Where(x => x.UserId == userId
                                    && x.ApplyToUpcomingAccounts
                                    && x.Id != keepDefaultMessageId)
                                .ToListAsync(cancellationToken);

            foreach (var dm in others)
            {
                dm.ApplyToUpcomingAccounts = false;
            }
        }

        public static async Task<string?> GetUpcomingMessageAsync(ApplicationDbContext db, int userId, CancellationToken cancellationToken)
        {
            return await db.DefaultMessages
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.ApplyToUpcomingAccounts)
                .Select(x => x.Message)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Specific linked message first; otherwise the user's upcoming message (may be null).
        /// </summary>
        public static string? ResolveMessage(Account account, string? upcomingMessage)
        {
            if (account.DefaultMessage != null)
            {
                return account.DefaultMessage.Message;
            }

            return upcomingMessage;
        }
    }
}
