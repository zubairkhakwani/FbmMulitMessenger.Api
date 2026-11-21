using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FBMMultiMessenger.Buisness.Helpers
{
    /// <summary>
    /// Holds the editable Gemini system prompt in memory after a one-time load from Settings.
    /// Reloaded when the portal saves a new prompt (via the API reload endpoint).
    /// The mandatory I/O appendix is appended at call time via <see cref="GeminiSystemPromptDefaults.BuildFullPrompt"/>.
    /// </summary>
    public class GeminiSystemPromptCache
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private string? _prompt;

        /// <summary>Editable prompt only (portal/DB). Does not include the mandatory I/O appendix.</summary>
        public string Get() => _prompt ?? GeminiSystemPromptDefaults.SystemPrompt;


        public async Task WarmAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await LoadFromDbUnlockedAsync(db, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task ReloadAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                await LoadFromDbUnlockedAsync(dbContext, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task LoadFromDbUnlockedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
        {
            var settings = await dbContext.Settings.FirstOrDefaultAsync(cancellationToken);

            if (settings is null)
            {
                settings = new Settings
                {
                    Extension_Version = Guid.NewGuid().ToString(),
                    AiSystemPrompt = GeminiSystemPromptDefaults.SystemPrompt,
                    CreatedAt = DateTime.UtcNow,
                };

                dbContext.Settings.Add(settings);

                await dbContext.SaveChangesAsync(cancellationToken);

                _prompt = GeminiSystemPromptDefaults.SystemPrompt;

                return;
            }

            if (string.IsNullOrWhiteSpace(settings.AiSystemPrompt))
            {
                settings.AiSystemPrompt = GeminiSystemPromptDefaults.SystemPrompt;

                settings.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                _prompt = GeminiSystemPromptDefaults.SystemPrompt;

                return;
            }

            _prompt = settings.AiSystemPrompt.Trim();
        }
    }
}
