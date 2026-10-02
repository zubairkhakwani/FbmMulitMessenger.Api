using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models.SignalR.Extension;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.StatusBatching;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Data.DB;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public class ChatHub : Hub
    {
        public static ConcurrentDictionary<string, string> _devices = new ConcurrentDictionary<string, string>();
        private readonly IAccountStatusQueue _accountStatusQueue;
        private readonly ApplicationDbContext _dbContext;
        private readonly AccountActiveStatusCache _accountActiveStatusCache;
        private readonly IConfiguration _configuration;

        public ChatHub(IAccountStatusQueue accountStatusQueue, ApplicationDbContext dbContext, AccountActiveStatusCache accountActiveStatusCache, IConfiguration configuration)
        {
            this._accountStatusQueue = accountStatusQueue;
            this._dbContext = dbContext;
            this._accountActiveStatusCache = accountActiveStatusCache;
            this._configuration = configuration;
        }

        // Per-day connectivity log file so no single file grows unbounded.
        private static string ConnectivityLogFile => $"extension-connectivity-{DateTime.UtcNow:yyyy-MM-dd}.log";

        public async Task RegisterLocalServer(string localServerId)
        {
            try
            {
                var metadata = new ConnectionMetadata()
                {
                    UserId = localServerId,
                    IsLocalServer = true,
                    ConnectedAt = DateTime.UtcNow
                };

                SingnalRConnectionManager._connections[Context.ConnectionId] = metadata;

                await Groups.AddToGroupAsync(Context.ConnectionId, localServerId);

                await Groups.AddToGroupAsync(Context.ConnectionId, "AllServers");

                //await _localServerService.HandleServerOnlineAsync(localServerId);

                Console.WriteLine($"User with id {localServerId} connected");
            }

            catch (Exception ex)
            {

            }
        }

        public async Task RegisterApp(string appId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(appId))
                {
                    SingnalRConnectionManager._connections[Context.ConnectionId] = new ConnectionMetadata() { UserId = appId };

                    await Groups.AddToGroupAsync(Context.ConnectionId, appId);

                    Console.WriteLine($"User with id {appId} connected");
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task RegisterExtension(ExtensionConnectionSignalRModel request)
        {
            var accountId = request.AccountId;
            var apiUserId = request.UserId;

            try
            {
                // Reject accounts that were removed (soft-deleted): don't register, and tell the caller
                // to stop so it stops reconnecting/syncing. Cached (5-min TTL) so connect bursts don't
                // hit the DB per connection; removal updates the cache immediately.
                var isActiveAccount = await _accountActiveStatusCache.IsActiveAsync(accountId,
                    () => _dbContext.Accounts.AnyAsync(a => a.Id == accountId && a.IsActive));

                if (!isActiveAccount)
                {
                    await Clients.Caller.SendAsync("HandleAccountDeactivated", accountId);
                    Console.WriteLine($"Extension register rejected — account {accountId} is not active.");
                    return;
                }

                // Ownership backstop: the socket's apiUserId is client-supplied, so a swapped API key can
                // register with a stale AccountId owned by the previous user — presence would then be written
                // under the wrong user and dropped by the flusher's ownership guard (stuck-offline). Reject the
                // mismatch and tell the caller to re-register under the current identity.
                //var ownsAccount = await _dbContext.Accounts.AnyAsync(a => a.Id == accountId && a.UserId == apiUserId);

                //if (!ownsAccount)
                //{
                //    await Clients.Caller.SendAsync("HandleReRegister", accountId);
                //    Console.WriteLine($"Extension register rejected — account {accountId} not owned by user {apiUserId}.");
                //    return;
                //}

                var extensionId = $"extension_{accountId}";

                SingnalRConnectionManager._connections[Context.ConnectionId] = new ConnectionMetadata() { ExtensionId = extensionId, AccountId = accountId, APIUserId = apiUserId, ConnectedAt = DateTime.UtcNow };

                await Groups.AddToGroupAsync(Context.ConnectionId, extensionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, "AllExtensinos");

                // Presence is owned by the hub: queue an "online" change (batched write).
                _accountStatusQueue.Enqueue(new AccountStatusChange
                {
                    AccountId = accountId,
                    UserId = apiUserId,
                    ConnectionStatus = AccountConnectionStatus.Online,
                    IsExtensionConnected = true,
                    AuthStatus = AccountAuthStatus.LoggedIn,
                    Reason = AccountReason.ConnectedWithExtension,
                    AtUtc = DateTime.UtcNow,
                });

                // Diagnostic: log the connect into the per-day connectivity file, so the connect/disconnect
                // timeline per account can be read together (spot flapping, hold durations, etc.).
                if (_configuration.GetValue("Diagnostics:LogSignalRConnect", true))
                {
                    DiagnosticFileLogger.Append(ConnectivityLogFile,
                        $"CONNECTED    account={accountId} user={apiUserId} conn={Context.ConnectionId}");
                }

                Console.WriteLine($"Extension with id {extensionId} connected");
            }
            catch (Exception ex)
            {

            }
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionMetadata = SingnalRConnectionManager._connections.FirstOrDefault(x => x.Key == Context.ConnectionId).Value;

            if (connectionMetadata != null)
            {
                SingnalRConnectionManager._connections.TryRemove(Context.ConnectionId, out var _);

                var userId = connectionMetadata.UserId;
                //means it is the extension that is disconnecting
                if (connectionMetadata.AccountId != null && connectionMetadata.APIUserId != null)
                {
                    // A gone extension has no login state, so a disconnect owns both dimensions.
                    _accountStatusQueue.Enqueue(new AccountStatusChange
                    {
                        AccountId = connectionMetadata.AccountId.Value,
                        UserId = connectionMetadata.APIUserId.Value,
                        ConnectionStatus = AccountConnectionStatus.Offline,
                        IsExtensionConnected = false,
                        AuthStatus = AccountAuthStatus.NotConnected,
                        Reason = AccountReason.NotConnected,
                        AtUtc = DateTime.UtcNow,
                    });

                    // Diagnostic: record WHY this extension socket closed, so per-account flapping can be
                    // classified — a clean close (exception == null: page reload / client stop) vs an error
                    // close (timeout / transport failure carries an exception). Connection duration helps spot
                    // a regular timeout cadence. Written to the per-day connectivity file (viewable via the
                    // diagnostics endpoint).
                    if (_configuration.GetValue("Diagnostics:LogSignalRDisconnect", true))
                    {
                        var heldSeconds = connectionMetadata.ConnectedAt == default
                            ? (double?)null
                            : (DateTime.UtcNow - connectionMetadata.ConnectedAt).TotalSeconds;

                        var closeKind = exception == null ? "clean (no exception)" : $"error: {exception.GetType().Name}: {exception.Message}";

                        DiagnosticFileLogger.Append(ConnectivityLogFile,
                            $"DISCONNECTED account={connectionMetadata.AccountId.Value} user={connectionMetadata.APIUserId.Value} " +
                            $"conn={Context.ConnectionId} held={heldSeconds?.ToString("0.0") ?? "?"}s close=[{closeKind}]");
                    }
                }

            }
            await base.OnDisconnectedAsync(exception);
        }

        public class ConnectionMetadata
        {
            public string UserId { get; set; } = string.Empty;
            public int? APIUserId { get; set; }
            public string ExtensionId { get; set; } = string.Empty;
            public int? AccountId { get; set; }
            public bool IsLocalServer { get; set; }
            public DateTime ConnectedAt { get; set; }
        }
    }
}
