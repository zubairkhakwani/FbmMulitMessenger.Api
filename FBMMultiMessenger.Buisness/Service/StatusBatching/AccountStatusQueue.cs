using System.Threading.Channels;
using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Buisness.Service.StatusBatching
{
    /// <summary>
    /// A single account status change. Every field except the identifiers is nullable:
    /// null means "leave this column unchanged". This lets producers that only observe one
    /// dimension (presence vs auth) enqueue without clobbering the other.
    /// </summary>
    public record AccountStatusChange
    {
        public int AccountId { get; init; }
        public int UserId { get; init; }

        // Presence dimension (owned by the SignalR hub).
        public AccountConnectionStatus? ConnectionStatus { get; init; }
        public bool? IsExtensionConnected { get; init; }

        // Auth dimension (owned by the HTTP auth-check path). A disconnect also sets these,
        // because a gone extension has no login state.
        public AccountAuthStatus? AuthStatus { get; init; }
        public AccountReason? Reason { get; init; }

        public DateTime AtUtc { get; init; } = DateTime.UtcNow;
    }

    public interface IAccountStatusQueue
    {
        void Enqueue(AccountStatusChange change);
        ChannelReader<AccountStatusChange> Reader { get; }
    }

    /// <summary>
    /// In-memory queue of account status changes, drained and batched by <see cref="AccountStatusFlushService"/>.
    /// Bounded so a runaway burst can't grow memory without limit; DropOldest is safe because the flusher
    /// coalesces to the latest state per account anyway, and any dropped update is corrected by the next event.
    /// </summary>
    public class AccountStatusQueue : IAccountStatusQueue
    {
        private readonly Channel<AccountStatusChange> _channel =
            Channel.CreateBounded<AccountStatusChange>(new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

        public ChannelReader<AccountStatusChange> Reader => _channel.Reader;

        public void Enqueue(AccountStatusChange change) => _channel.Writer.TryWrite(change);
    }
}
