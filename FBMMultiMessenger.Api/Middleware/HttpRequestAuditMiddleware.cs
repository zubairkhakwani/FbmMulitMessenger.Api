using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FBMMultiMessenger.Buisness.Helpers;

namespace FBMMultiMessenger.Api.Middleware;

/// <summary>
/// Writes one JSON line per HTTP request to Logs/http-request-audit-{date}.log.
/// Full bodies when Diagnostics:HttpRequestAuditMaxBodyChars is 0; set a positive value to cap bodies.
/// </summary>
public sealed class HttpRequestAuditMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public HttpRequestAuditMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsEnabled() || ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request);

        var originalResponseBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            context.Response.Body = originalResponseBody;

            var responseBytes = responseBuffer.ToArray();
            var responseText = DecodeBody(responseBytes, context.Response.ContentType);

            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalResponseBody);

            TryWriteAuditLine(context, requestBody, responseText, responseBytes.Length, sw.ElapsedMilliseconds);
        }
    }

    private bool IsEnabled() =>
        _configuration.GetValue("Diagnostics:HttpRequestAuditEnabled", false);

    /// <summary>0 = log full body; positive = max characters stored in the audit file.</summary>
    private int MaxBodyChars() =>
        _configuration.GetValue("Diagnostics:HttpRequestAuditMaxBodyChars", 0);

    private bool ShouldSkip(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            return true;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/sys", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        //if (path.StartsWith("/chathub", StringComparison.OrdinalIgnoreCase))
        //{
        //    return true;
        //}

        return false;
    }

    private static async Task<(string Text, int SizeBytes)> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == 0 && request.Body.CanSeek && request.Body.Length == 0)
        {
            return ("", 0);
        }

        request.EnableBuffering();
        request.Body.Position = 0;

        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms);
        var bytes = ms.ToArray();

        request.Body.Position = 0;
        return (DecodeBody(bytes, request.ContentType), bytes.Length);
    }

    private static string DecodeBody(byte[] bytes, string? contentType)
    {
        if (bytes.Length == 0)
        {
            return "";
        }

        var isMostlyText = contentType == null
            || contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("text", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);

        if (isMostlyText)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        return Convert.ToBase64String(bytes);
    }

    private static (string Stored, bool Truncated) PrepareBodyForLog(string fullText, int maxChars)
    {
        if (maxChars <= 0 || fullText.Length <= maxChars)
        {
            return (fullText, false);
        }

        return (fullText[..maxChars], true);
    }

    private void TryWriteAuditLine(
        HttpContext context,
        (string Text, int SizeBytes) request,
        string responseText,
        int responseSizeBytes,
        long durationMs)
    {
        try
        {
            var maxChars = MaxBodyChars();
            var (reqBody, reqTrunc) = PrepareBodyForLog(request.Text, maxChars);
            var (resBody, resTrunc) = PrepareBodyForLog(responseText, maxChars);

            var utc = DateTime.UtcNow;
            DateTimeOffset local;
            try
            {
                var tzId = _configuration["Diagnostics:AuditDisplayTimeZone"];
                var tz = string.IsNullOrWhiteSpace(tzId)
                    ? TimeZoneInfo.Local
                    : TimeZoneInfo.FindSystemTimeZoneById(tzId);
                local = TimeZoneInfo.ConvertTime(new DateTimeOffset(utc, TimeSpan.Zero), tz);
            }
            catch
            {
                local = new DateTimeOffset(utc, TimeSpan.Zero);
            }

            int? userId = null;
            var email = "";
            var user = context.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var idClaim = user.FindFirst("Id")?.Value;
                if (int.TryParse(idClaim, out var id))
                {
                    userId = id;
                }

                email = user.FindFirst(ClaimTypes.Email)?.Value ?? "";
            }

            var record = new
            {
                timeUtc = utc.ToString("O"),
                timeLocal = local.ToString("O"),
                userId,
                userEmail = email,
                method = context.Request.Method,
                path = context.Request.Path.Value ?? "",
                queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "",
                statusCode = context.Response.StatusCode,
                durationMs,
                requestSizeBytes = request.SizeBytes,
                responseSizeBytes = responseSizeBytes,
                requestBody = reqBody,
                responseBody = resBody,
                requestBodyTruncated = reqTrunc,
                responseBodyTruncated = resTrunc,
            };

            var line = JsonSerializer.Serialize(record, JsonOptions);
            var fileName = $"http-request-audit-{utc:yyyy-MM-dd}.log";
            DiagnosticFileLogger.AppendRawLine(fileName, line);
        }
        catch
        {
            // Never break the request because audit failed.
        }
    }
}

public static class HttpRequestAuditMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpRequestAudit(this IApplicationBuilder app) =>
        app.UseMiddleware<HttpRequestAuditMiddleware>();
}
