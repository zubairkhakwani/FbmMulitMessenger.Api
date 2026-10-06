using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Data.DB;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FBMMultiMessenger.Buisness.Service
{
    /// <summary>
    /// Sends FCM web push to admin browsers registered via the Portal.
    /// No-ops safely when Firebase is not configured or no tokens exist.
    /// </summary>
    internal class FirebaseAdminPushService : IAdminPushNotificationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly FirebaseSettings _settings;
        private static readonly object InitLock = new();
        private static bool _initAttempted;

        public FirebaseAdminPushService(
            ApplicationDbContext dbContext,
            IOptions<FirebaseSettings> settings)
        {
            _dbContext = dbContext;
            _settings = settings.Value;
        }

        public async Task NotifyPaymentProofSubmittedAsync(
            string userName,
            string email,
            string billingCycle,
            decimal purchasedPrice,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled)
                return;

            if (!EnsureFirebaseApp())
                return;

            var tokens = await _dbContext.AdminFcmTokens
                .AsNoTracking()
                .Select(t => t.Token)
                .Where(t => t != null && t != "")
                .Distinct()
                .ToListAsync(cancellationToken);

            if (tokens.Count == 0)
                return;

            var title = "New payment proof submitted";
            var body = $"{userName} ({email}) — {billingCycle} — Rs {purchasedPrice:0.##}";
            var clickUrl = string.IsNullOrWhiteSpace(_settings.ClickActionUrl)
                ? "/admin/payment/verifications"
                : _settings.ClickActionUrl.Trim();

            // FCM allows up to 500 tokens per multicast.
            foreach (var batch in tokens.Chunk(500))
            {
                var message = new MulticastMessage
                {
                    Tokens = batch.ToList(),
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body,
                    },
                    Webpush = new WebpushConfig
                    {
                        FcmOptions = new WebpushFcmOptions
                        {
                            Link = clickUrl,
                        },
                        Notification = new WebpushNotification
                        {
                            Title = title,
                            Body = body,
                        },
                        Data = new Dictionary<string, string>
                        {
                            { "type", "payment_proof" },
                            { "url", clickUrl },
                        },
                    },
                };

                try
                {
                    var response = await FirebaseMessaging.DefaultInstance
                        .SendEachForMulticastAsync(message, cancellationToken);

                    await RemoveInvalidTokensAsync(batch, response, cancellationToken);
                }
                catch (Exception ex)
                {
                    TryLog($"FCM send failed: {ex.Message}");
                }
            }
        }

        private async Task RemoveInvalidTokensAsync(
            IReadOnlyList<string> batch,
            BatchResponse response,
            CancellationToken cancellationToken)
        {
            if (response.FailureCount == 0)
                return;

            var stale = new List<string>();
            for (var i = 0; i < response.Responses.Count; i++)
            {
                var r = response.Responses[i];
                if (r.IsSuccess)
                    continue;

                var code = r.Exception?.MessagingErrorCode;
                if (code == MessagingErrorCode.Unregistered ||
                    code == MessagingErrorCode.InvalidArgument)
                {
                    stale.Add(batch[i]);
                }
            }

            if (stale.Count == 0)
                return;

            var rows = await _dbContext.AdminFcmTokens
                .Where(t => stale.Contains(t.Token))
                .ToListAsync(cancellationToken);
            if (rows.Count == 0)
                return;

            _dbContext.AdminFcmTokens.RemoveRange(rows);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private bool EnsureFirebaseApp()
        {
            if (FirebaseApp.DefaultInstance != null)
                return true;

            lock (InitLock)
            {
                if (FirebaseApp.DefaultInstance != null)
                    return true;

                if (_initAttempted)
                    return false;

                _initAttempted = true;

                try
                {
                    if (!_settings.Enabled)
                        return false;

                    var credentialPath = ResolveCredentialPath();
                    if (credentialPath == null)
                    {
                        TryLog("Firebase CredentialsPath file not found — admin push disabled.");
                        return false;
                    }

                    var credential = GoogleCredential.FromFile(credentialPath);
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = credential,
                    });
                    return true;
                }
                catch (Exception ex)
                {
                    TryLog($"Firebase init failed: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Resolves CredentialsPath against the app base directory / current directory.
        /// </summary>
        private string? ResolveCredentialPath()
        {
            var configured = _settings.CredentialsPath?.Trim();
            if (string.IsNullOrWhiteSpace(configured))
                return null;

            if (Path.IsPathRooted(configured) && File.Exists(configured))
                return configured;

            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, configured),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configured)),
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private static void TryLog(string message)
        {
            try
            {
                if (!Directory.Exists("Logs"))
                    Directory.CreateDirectory("Logs");
                var file = $"Logs\\fcm-{DateTime.Now:yyyy-MM-dd}.txt";
                File.AppendAllText(file, $"{DateTime.Now:O} {message}{Environment.NewLine}");
            }
            catch
            {
                // ignore logging failures
            }
        }
    }
}
