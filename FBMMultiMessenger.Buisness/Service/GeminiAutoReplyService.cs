using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Data.Database.DbModels;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FBMMultiMessenger.Buisness.Service
{
    /// <summary>
    /// Generates a short Marketplace reply from the seller's product knowledge base via Gemini.
    /// </summary>
    public class GeminiAutoReplyService
    {
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };


        private readonly GeminiSettings _settings;
        private readonly GeminiSystemPromptCache _promptCache;

        public GeminiAutoReplyService(IOptions<GeminiSettings> settings, GeminiSystemPromptCache promptCache)
        {
            _settings = settings.Value;
            _promptCache = promptCache;
        }

        private static string LogFile => $"gemini-auto-reply-{DateTime.UtcNow:yyyy-MM-dd}.log";

        /// <summary>Max prior messages (excluding the triggering buyer message) sent as conversation history.</summary>
        public const int MaxHistoryMessages = 25;

        private static readonly int[] RetryDelaysSeconds = [3, 5];
        private const int MaxAttempts = 3;

        public async Task<GeminiAutoReplyResult?> TryGenerateReplyAsync(
            Chat chat,
            IReadOnlyList<GeminiProductPromptItem> products,
            string buyerMessage,
            IReadOnlyList<ChatMessages> recentMessages,
            string correlationId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                return null;
            }

            if (products.Count == 0 || products.All(p => p.Qas.Count == 0))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(buyerMessage))
            {
                return null;
            }

            var contextPrompt = BuildContextPrompt(chat, products);
            var contents = BuildContents(contextPrompt, recentMessages, buyerMessage.Trim());
            var model = string.IsNullOrWhiteSpace(_settings.Model)
                ? "gemini-3.5-flash-lite"
                : _settings.Model.Trim();
            // Editable (portal/DB) prompt + hardcoded mandatory I/O contract.
            var systemPrompt = GeminiSystemPromptDefaults.BuildFullPrompt(_promptCache.Get());

            var body = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } },
                },
                contents,
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 512,
                    responseMimeType = "application/json",
                },
            };

            var requestUrl =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(_settings.ApiKey)}";

            string? lastError = null;
            Exception? lastException = null;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using var response = await Http.PostAsJsonAsync(
                        requestUrl,
                        body,
                        JsonOptions,
                        cancellationToken);

                    var raw = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        lastError = $"Gemini HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                        lastException = null;
                    }
                    else
                    {
                        var (parsed, parseError) = TryParseGeminiHttpResponse(raw);
                        if (parsed is not null)
                        {
                            return parsed;
                        }

                        lastError = parseError ?? "Gemini response parse failed";
                        lastException = null;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = $"{ex.GetType().Name}: {ex.Message}";
                    lastException = ex;
                }

                if (attempt < MaxAttempts)
                {
                    var delaySeconds = RetryDelaysSeconds[attempt - 1];
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
            }

            var failDetail =
                $"chatId={chat.Id} correlationId={correlationId} model={model} attempts={MaxAttempts} error={lastError}";
            DiagnosticFileLogger.Append(LogFile, $"[ERROR] {failDetail}");

            if (lastException is not null)
            {
                SentrySdk.CaptureException(lastException, scope =>
                {
                    scope.SetTag("feature", "gemini-auto-reply");
                    scope.SetTag("chatId", chat.Id.ToString());
                    scope.SetTag("correlationId", correlationId);
                    scope.SetExtra("attempts", MaxAttempts);
                    scope.SetExtra("lastError", lastError);
                });
            }
            else
            {
                SentrySdk.CaptureMessage(
                    $"Gemini auto-reply failed after {MaxAttempts} attempts: {lastError}",
                    scope =>
                    {
                        scope.SetTag("feature", "gemini-auto-reply");
                        scope.SetTag("chatId", chat.Id.ToString());
                        scope.SetTag("correlationId", correlationId);
                        scope.SetExtra("attempts", MaxAttempts);
                        scope.SetExtra("lastError", lastError);
                        scope.Level = SentryLevel.Error;
                    });
            }

            return null;
        }

        private static (GeminiAutoReplyResult? Result, string? Error) TryParseGeminiHttpResponse(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);

                if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
                    || candidates.GetArrayLength() == 0)
                {
                    return (null, "No candidates in Gemini response");
                }

                var first = candidates[0];
                if (!first.TryGetProperty("content", out var content)
                    || !content.TryGetProperty("parts", out var parts)
                    || parts.GetArrayLength() == 0)
                {
                    return (null, "No content.parts in first candidate");
                }

                var text = parts[0].TryGetProperty("text", out var textEl)
                    ? textEl.GetString()
                    : null;

                text = text?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return (null, "Parsed reply text is empty");
                }

                return TryParseReplyJson(text);
            }
            catch (Exception ex)
            {
                return (null, $"Invalid Gemini JSON envelope: {ex.Message}");
            }
        }

        private static (GeminiAutoReplyResult? Result, string? Error) TryParseReplyJson(string text)
        {
            var json = UnwrapJsonPayload(text);
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var message = root.TryGetProperty("message", out var msgEl)
                    ? msgEl.GetString()?.Trim() ?? string.Empty
                    : string.Empty;

                var delay = 0;
                if (root.TryGetProperty("delaySeconds", out var delayEl))
                {
                    delay = delayEl.ValueKind switch
                    {
                        JsonValueKind.Number => delayEl.TryGetInt32(out var d) ? d : 0,
                        JsonValueKind.String => int.TryParse(delayEl.GetString(), out var ds) ? ds : 0,
                        _ => 0,
                    };
                }

                var sendImages = false;
                if (root.TryGetProperty("sendProductImages", out var imgEl))
                {
                    sendImages = imgEl.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.String => bool.TryParse(imgEl.GetString(), out var b) && b,
                        _ => false,
                    };
                }

                int? matchedProductId = null;
                if (root.TryGetProperty("matchedProductId", out var matchEl)
                    && matchEl.ValueKind != JsonValueKind.Null)
                {
                    matchedProductId = matchEl.ValueKind switch
                    {
                        JsonValueKind.Number => matchEl.TryGetInt32(out var id) ? id : null,
                        JsonValueKind.String => int.TryParse(matchEl.GetString(), out var ids) ? ids : null,
                        _ => null,
                    };
                }

                // Clamp delay; empty message forces 0 / no images.
                if (string.IsNullOrWhiteSpace(message))
                {
                    return (new GeminiAutoReplyResult
                    {
                        Message = string.Empty,
                        DelaySeconds = 0,
                        SendProductImages = false,
                        MatchedProductId = null,
                    }, null);
                }

                if (delay < 0) delay = 0;
                if (delay > 8) delay = 8;

                return (new GeminiAutoReplyResult
                {
                    Message = message,
                    DelaySeconds = delay,
                    SendProductImages = sendImages,
                    MatchedProductId = matchedProductId,
                }, null);
            }
            catch (Exception ex)
            {
                return (null, $"JSON parse failed: {ex.Message}");
            }
        }

        private static string UnwrapJsonPayload(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = trimmed.IndexOf('\n');
                if (firstNewline >= 0)
                {
                    trimmed = trimmed[(firstNewline + 1)..];
                }

                var fence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (fence >= 0)
                {
                    trimmed = trimmed[..fence];
                }

                trimmed = trimmed.Trim();
            }

            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                return trimmed[start..(end + 1)];
            }

            return trimmed;
        }

        private static string BuildContextPrompt(
            Chat chat,
            IReadOnlyList<GeminiProductPromptItem> products)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Listing title: " + (chat.FbListingTitle ?? "(unknown)"));

            if (!string.IsNullOrWhiteSpace(chat.FbListingLocation))
            {
                sb.AppendLine("Listing location: " + chat.FbListingLocation);
            }

            sb.AppendLine();
            sb.AppendLine("Match the chat listing title against each product's listing titles below when present (ignore product display names for matching).");
            sb.AppendLine("Use only the matched product's knowledge base. Return that product's id as matchedProductId.");
            sb.AppendLine("If a product has no listing titles it cannot win a title match; a single knowledge product may still be used.");
            sb.AppendLine("Conversation history follows after this context (buyer = user, seller = model). Reply only to the latest buyer message.");
            sb.AppendLine();

            foreach (var product in products)
            {
                sb.AppendLine($"## Product id={product.ProductId}");
                sb.AppendLine("Listing titles for this product:");
                if (product.ListingTitles.Count == 0)
                {
                    sb.AppendLine("  (none — no listing titles)");
                }
                else
                {
                    foreach (var title in product.ListingTitles)
                    {
                        sb.AppendLine($"  - {title}");
                    }
                }

                sb.AppendLine($"Photos available to attach: {product.ImageCount}");
                if (product.ImageCount == 0)
                {
                    sb.AppendLine("Note: No photos stored for this product. Prefer sendProductImages false unless the buyer asks.");
                }

                sb.AppendLine("Knowledge base (use this information as needed):");
                if (product.Qas.Count == 0)
                {
                    sb.AppendLine("  (none)");
                }
                else
                {
                    foreach (var qa in product.Qas)
                    {
                        var topics = ProductQaHelper.DeserializeQuestions(qa.QuestionVariantsJson);
                        if (topics.Count > 0)
                        {
                            sb.AppendLine("  Topics this information relates to:");
                            foreach (var topic in topics)
                            {
                                sb.AppendLine($"    - {topic}");
                            }
                        }

                        sb.AppendLine($"  Information: {qa.Answer}");
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Builds Gemini multi-turn contents: context, then history (media as placeholders), then latest buyer message.
        /// Consecutive same-role turns are merged so Gemini always sees valid alternation.
        /// </summary>
        private static List<object> BuildContents(
            string contextPrompt,
            IReadOnlyList<ChatMessages> recentMessages,
            string buyerMessage)
        {
            var turns = new List<(string Role, StringBuilder Text)>();

            void Append(string role, string text)
            {
                if (string.IsNullOrWhiteSpace(text)) return;

                if (turns.Count > 0 && turns[^1].Role == role)
                {
                    turns[^1].Text.AppendLine();
                    turns[^1].Text.Append(text.Trim());
                }
                else
                {
                    turns.Add((role, new StringBuilder(text.Trim())));
                }
            }

            Append("user", contextPrompt);

            // Exclude the triggering inbound message when it is already the last row.
            var prior = recentMessages;
            if (prior.Count > 0 && prior[^1].IsReceived)
            {
                prior = prior.Take(prior.Count - 1).ToList();
            }

            foreach (var msg in prior)
            {
                var role = msg.IsReceived ? "user" : "model";
                Append(role, FormatHistoryLine(msg));
            }

            Append("user", "Latest buyer message:\n" + buyerMessage);

            return turns
                .Select(t => (object)new
                {
                    role = t.Role,
                    parts = new[] { new { text = t.Text.ToString() } },
                })
                .ToList();
        }

        private static string FormatHistoryLine(ChatMessages msg)
        {
            var who = msg.IsReceived ? "Buyer" : "Seller";

            if (msg.IsImageMessage)
            {
                var count = CountMediaItems(msg.Message);
                return count > 1
                    ? $"[{who} sent {count} photos]"
                    : $"[{who} sent a photo]";
            }

            if (msg.IsVideoMessage)
            {
                var count = CountMediaItems(msg.Message);
                return count > 1
                    ? $"[{who} sent {count} videos]"
                    : $"[{who} sent a video]";
            }

            if (msg.IsAudioMessage)
            {
                return $"[{who} sent a voice note]";
            }

            var text = (msg.Message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return $"[{who} sent a message]";
            }

            return $"{who}: {text}";
        }

        private static int CountMediaItems(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return 1;

            var trimmed = message.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                try
                {
                    var urls = JsonSerializer.Deserialize<List<string>>(trimmed);
                    if (urls is { Count: > 0 }) return urls.Count;
                }
                catch
                {
                    // Fall through.
                }
            }

            return 1;
        }
    }
}
