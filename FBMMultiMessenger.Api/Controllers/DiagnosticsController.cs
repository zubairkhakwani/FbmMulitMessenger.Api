using FBMMultiMessenger.Buisness.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace FBMMultiMessenger.Api.Controllers
{
    /// <summary>
    /// Unauthenticated log viewer, protected only by an unguessable path segment configured in
    /// Diagnostics:AccessKey (security-by-obscurity — set a long random value in production; if the key
    /// is not configured every route returns 404, i.e. the feature is off). Serves a self-contained
    /// HTML page plus JSON endpoints that list and read files under the app's Logs/ directory only
    /// (path-sandboxed — no traversal outside Logs/).
    /// </summary>
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DiagnosticsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private static string LogsDir => Path.Combine(Directory.GetCurrentDirectory(), "Logs");

        private bool KeyValid(string key)
        {
            var configured = _configuration["Diagnostics:AccessKey"];
            return !string.IsNullOrWhiteSpace(configured) && string.Equals(configured, key, StringComparison.Ordinal);
        }

        // The viewer page. JS derives all data-endpoint URLs from location.pathname, so the key travels
        // with every request without being embedded here.
        [HttpGet("/api/sys/{key}")]
        public IActionResult Viewer(string key)
        {
            if (!KeyValid(key)) return NotFound();
            return Content(HtmlPage, "text/html");
        }

        // Lists the files in Logs/, newest first.
        [HttpGet("/api/sys/{key}/files")]
        public IActionResult Files(string key)
        {
            if (!KeyValid(key)) return NotFound();

            var dir = LogsDir;
            if (!Directory.Exists(dir))
            {
                return Ok(Array.Empty<object>());
            }

            var files = new DirectoryInfo(dir)
                .GetFiles()
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Select(f => new { name = f.Name, size = f.Length, modifiedUtc = f.LastWriteTimeUtc })
                .ToList();

            return Ok(files);
        }

        // Returns the content of one file in Logs/. Large files are tailed to the last ~5 MB. With
        // download=true, streams the whole file as an attachment.
        [HttpGet("/api/sys/{key}/content")]
        public IActionResult FileContent(string key, [FromQuery] string? file, [FromQuery] bool download = false)
        {
            if (!KeyValid(key)) return NotFound();

            // Path safety: only a bare file name that lives directly inside Logs/ is allowed.
            var safeName = Path.GetFileName(file ?? string.Empty);
            if (string.IsNullOrEmpty(safeName) || safeName != file)
            {
                return BadRequest("Invalid file name.");
            }

            var fullPath = Path.Combine(LogsDir, safeName);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            if (download)
            {
                var bytes = System.IO.File.ReadAllBytes(fullPath);
                return File(bytes, "application/octet-stream", safeName);
            }

            const long maxBytes = 5 * 1024 * 1024; // cap the response; tail the rest
            long size;
            bool truncated = false;
            string text;

            using (var fs = System.IO.File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                size = fs.Length;
                if (size > maxBytes)
                {
                    fs.Seek(size - maxBytes, SeekOrigin.Begin);
                    truncated = true;
                }
                using var reader = new StreamReader(fs);
                text = reader.ReadToEnd();
            }

            return Ok(new { name = safeName, size, truncated, text });
        }

        // Empties one file in Logs/ (keeps the file so logging continues appending).
        [HttpPost("/api/sys/{key}/clear")]
        public IActionResult Clear(string key, [FromQuery] string? file)
        {
            if (!KeyValid(key)) return NotFound();

            var safeName = Path.GetFileName(file ?? string.Empty);
            if (string.IsNullOrEmpty(safeName) || safeName != file)
            {
                return BadRequest("Invalid file name.");
            }

            var fullPath = Path.Combine(LogsDir, safeName);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            DiagnosticFileLogger.Clear(fullPath);
            return Ok(new { cleared = safeName });
        }

        [HttpGet("/api/sys/{key}/http-request-audit")]
        public IActionResult HttpRequestAuditViewer(string key)
        {
            if (!KeyValid(key)) return NotFound();
            return Content(HttpRequestAuditHtmlPage, "text/html");
        }

        // Paginated rows from one http-request-audit-*.log file (JSONL).
        [HttpGet("/api/sys/{key}/http-request-audit/rows")]
        public IActionResult HttpRequestAuditRows(
            string key,
            [FromQuery] string? file,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? userId = null,
            [FromQuery] string? userEmail = null,
            [FromQuery] string? method = null,
            [FromQuery] string? pathContains = null,
            [FromQuery] int? statusCode = null,
            [FromQuery] string? fromUtc = null,
            [FromQuery] string? toUtc = null,
            [FromQuery] string? search = null,
            [FromQuery] bool newestFirst = true,
            [FromQuery] int tail = 0,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDir = null)
        {
            if (!KeyValid(key)) return NotFound();

            var safeName = Path.GetFileName(file ?? string.Empty);
            if (string.IsNullOrEmpty(safeName) || safeName != file
                || !safeName.StartsWith("http-request-audit-", StringComparison.Ordinal)
                || !safeName.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid audit file name.");
            }

            var fullPath = Path.Combine(LogsDir, safeName);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            DateTimeOffset? from = null;
            DateTimeOffset? to = null;
            if (DateTimeOffset.TryParse(fromUtc, out var parsedFrom)) from = parsedFrom.ToUniversalTime();
            if (DateTimeOffset.TryParse(toUtc, out var parsedTo)) to = parsedTo.ToUniversalTime();

            var emailFilter = userEmail?.Trim();
            var methodFilter = method?.Trim();
            var pathFilter = pathContains?.Trim();
            var searchFilter = search?.Trim();

            var columns = new[]
            {
                "timeUtc", "timeLocal", "userId", "userEmail", "method", "path", "queryString",
                "statusCode", "durationMs", "requestSizeBytes", "responseSizeBytes",
                "requestBodyTruncated", "responseBodyTruncated", "requestBody", "responseBody"
            };

            List<Dictionary<string, object?>> matched;

            if (string.IsNullOrEmpty(searchFilter))
            {
                // Fast path: the common filter/sort/page operations never look inside bodies, so serve them
                // from a per-file metadata cache (parsed once per file state, bodies skipped). Repeated
                // filtering, sorting and paging on the same file then cost nothing but an in-memory scan.
                var all = GetCachedMetadataRows(fullPath, safeName);
                matched = all
                    .Where(r => MetaRowMatches(r, userId, emailFilter, methodFilter, pathFilter, statusCode, from, to))
                    .ToList();
            }
            else
            {
                // Body search can't use the cache (bodies aren't cached), so read the file for this case and
                // parse full rows including bodies.
                matched = new List<Dictionary<string, object?>>();
                using var fs = System.IO.File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                string? line;
                var lineIndex = -1;
                while ((line = reader.ReadLine()) != null)
                {
                    lineIndex++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    JsonElement row;
                    try
                    {
                        row = JsonSerializer.Deserialize<JsonElement>(line);
                    }
                    catch
                    {
                        continue;
                    }

                    if (!RowMatchesFilters(row, userId, emailFilter, methodFilter, pathFilter, statusCode, from, to, searchFilter))
                    {
                        continue;
                    }

                    var dict = JsonRowToDictionary(row);
                    dict["__line"] = lineIndex;
                    matched.Add(dict);
                }
            }

            var sortCol = sortBy?.Trim();
            IEnumerable<Dictionary<string, object?>> ordered;
            if (!string.IsNullOrEmpty(sortCol) && columns.Contains(sortCol))
            {
                var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
                ordered = SortRows(matched, sortCol, desc);
            }
            else
            {
                ordered = newestFirst
                    ? matched.OrderByDescending(r => RowTimeUtc(r))
                    : matched.OrderBy(r => RowTimeUtc(r));
            }

            var list = ordered.ToList();
            tail = Math.Max(0, tail);
            if (tail > 0 && list.Count > tail)
            {
                list = newestFirst
                    ? list.Take(tail).ToList()
                    : list.Skip(list.Count - tail).Take(tail).ToList();
            }

            var total = list.Count;

            // Totals over the full filtered (+tail) set, so the UI can show aggregate sizes for the
            // current filters regardless of which page is visible.
            long totalRequestBytes = list.Sum(r => r.TryGetValue("requestSizeBytes", out var rv) && rv is int rvi ? rvi : 0);
            long totalResponseBytes = list.Sum(r => r.TryGetValue("responseSizeBytes", out var sv) && sv is int svi ? svi : 0);

            var skip = (page - 1) * pageSize;
            var pageRows = list.Skip(skip).Take(pageSize).ToList();

            return Ok(new { columns, rows = pageRows, total, totalRequestBytes, totalResponseBytes, page, pageSize, file = safeName, newestFirst, tail, sortBy = sortCol, sortDir });
        }

        // Returns one request/response body on demand (by physical line number), so the grid itself never
        // ships large bodies. `which` = "request" or "response".
        [HttpGet("/api/sys/{key}/http-request-audit/body")]
        public IActionResult HttpRequestAuditBody(string key, [FromQuery] string? file, [FromQuery] int line, [FromQuery] string? which)
        {
            if (!KeyValid(key)) return NotFound();

            var safeName = Path.GetFileName(file ?? string.Empty);
            if (string.IsNullOrEmpty(safeName) || safeName != file
                || !safeName.StartsWith("http-request-audit-", StringComparison.Ordinal)
                || !safeName.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid audit file name.");
            }

            var fullPath = Path.Combine(LogsDir, safeName);
            if (!System.IO.File.Exists(fullPath) || line < 0)
            {
                return NotFound();
            }

            string? target = null;
            using (var fs = System.IO.File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                var idx = -1;
                string? l;
                while ((l = reader.ReadLine()) != null)
                {
                    idx++;
                    if (idx == line) { target = l; break; }
                }
            }

            if (target is null)
            {
                return NotFound();
            }

            JsonElement row;
            try { row = JsonSerializer.Deserialize<JsonElement>(target); }
            catch { return NotFound(); }

            var isResponse = string.Equals(which, "response", StringComparison.OrdinalIgnoreCase);
            var field = isResponse ? "responseBody" : "requestBody";
            var truncField = isResponse ? "responseBodyTruncated" : "requestBodyTruncated";

            return Ok(new
            {
                which = field,
                body = GetString(row, field),
                truncated = GetBool(row, truncField),
            });
        }

        // Parsed metadata rows (bodies excluded) per audit file, invalidated when the file's size or
        // last-write time changes. The active file still re-parses whenever a new request is appended, but
        // every filter/sort/page on an unchanged file state is served from memory, and the parse itself is
        // body-skipping (see ParseMetadataLine) so even the active-file re-parse is cheap.
        private static readonly ConcurrentDictionary<string, AuditMetaCacheEntry> _auditMetaCache = new();

        private sealed class AuditMetaCacheEntry
        {
            public long Length;
            public long WriteTicks;
            public List<Dictionary<string, object?>> Rows = new();
        }

        private static List<Dictionary<string, object?>> GetCachedMetadataRows(string fullPath, string fileName)
        {
            var fi = new FileInfo(fullPath);
            var len = fi.Length;
            var ticks = fi.LastWriteTimeUtc.Ticks;

            if (_auditMetaCache.TryGetValue(fileName, out var hit) && hit.Length == len && hit.WriteTicks == ticks)
            {
                return hit.Rows;
            }

            var rows = new List<Dictionary<string, object?>>();
            using (var fs = System.IO.File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                string? line;
                var lineIndex = -1;
                while ((line = reader.ReadLine()) != null)
                {
                    lineIndex++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var dict = ParseMetadataLine(line);
                    if (dict == null) continue;
                    dict["__line"] = lineIndex;
                    rows.Add(dict);
                }
            }

            // Bound memory — this is a diagnostics tool, not a hot service. A handful of day-files is plenty.
            if (_auditMetaCache.Count > 6) _auditMetaCache.Clear();
            _auditMetaCache[fileName] = new AuditMetaCacheEntry { Length = len, WriteTicks = ticks, Rows = rows };
            return rows;
        }

        // Reads only the metadata fields from one JSONL line and skips the (potentially huge) body strings
        // without materializing them — much cheaper than deserializing the whole line into a JsonElement.
        private static Dictionary<string, object?>? ParseMetadataLine(string line)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(line);
                var reader = new Utf8JsonReader(bytes);
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return null;

                var d = new Dictionary<string, object?>();
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    var name = reader.GetString();
                    if (!reader.Read()) break;

                    switch (name)
                    {
                        case "timeUtc":
                        case "timeLocal":
                        case "userEmail":
                        case "method":
                        case "path":
                        case "queryString":
                            d[name] = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                            break;
                        case "userId":
                            d[name] = reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var ui) ? ui : (int?)null;
                            break;
                        case "statusCode":
                        case "requestSizeBytes":
                        case "responseSizeBytes":
                            d[name] = reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var iv) ? iv : 0;
                            break;
                        case "durationMs":
                            d[name] = reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var lv) ? lv : 0L;
                            break;
                        case "requestBodyTruncated":
                        case "responseBodyTruncated":
                            d[name] = reader.TokenType == JsonTokenType.True;
                            break;
                        default:
                            // requestBody / responseBody / anything else: skip the value without reading it.
                            reader.Skip();
                            break;
                    }
                }

                return d;
            }
            catch
            {
                return null;
            }
        }

        // Same predicate as RowMatchesFilters but over a pre-parsed metadata dictionary (no body search).
        private static bool MetaRowMatches(
            Dictionary<string, object?> r,
            int? userId,
            string? userEmail,
            string? method,
            string? pathContains,
            int? statusCode,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc)
        {
            if (userId.HasValue)
            {
                var u = r.TryGetValue("userId", out var uv) ? uv as int? : null;
                if (u != userId.Value) return false;
            }

            if (!string.IsNullOrEmpty(userEmail))
            {
                var email = r.TryGetValue("userEmail", out var ev) ? ev as string : null;
                if (email == null || !email.Contains(userEmail, StringComparison.OrdinalIgnoreCase)) return false;
            }

            if (!string.IsNullOrEmpty(method))
            {
                var m = r.TryGetValue("method", out var mv) ? mv as string : null;
                if (m == null || !string.Equals(m, method, StringComparison.OrdinalIgnoreCase)) return false;
            }

            if (!string.IsNullOrEmpty(pathContains))
            {
                var path = r.TryGetValue("path", out var pv) ? pv as string : null;
                if (path == null || !path.Contains(pathContains, StringComparison.OrdinalIgnoreCase)) return false;
            }

            if (statusCode.HasValue)
            {
                var s = r.TryGetValue("statusCode", out var sv) && sv is int si ? si : 0;
                if (s != statusCode.Value) return false;
            }

            if (fromUtc.HasValue || toUtc.HasValue)
            {
                var t = RowTimeUtc(r);
                if (fromUtc.HasValue && t < fromUtc.Value) return false;
                if (toUtc.HasValue && t > toUtc.Value) return false;
            }

            return true;
        }

        private static IEnumerable<Dictionary<string, object?>> SortRows(
            List<Dictionary<string, object?>> rows, string col, bool desc)
        {
            Func<Dictionary<string, object?>, IComparable?> key = col switch
            {
                "timeUtc" => r => RowTimeUtc(r),
                "userId" or "statusCode" or "durationMs" or "requestSizeBytes" or "responseSizeBytes"
                    or "requestBodyTruncated" or "responseBodyTruncated" => r => ToLong(r, col),
                _ => r => (r.TryGetValue(col, out var v) ? v as string : null) ?? string.Empty
            };

            return desc ? rows.OrderByDescending(key) : rows.OrderBy(key);
        }

        private static long ToLong(Dictionary<string, object?> r, string col)
        {
            if (!r.TryGetValue(col, out var v) || v is null) return long.MinValue;
            return v switch
            {
                long l => l,
                int i => i,
                bool b => b ? 1 : 0,
                _ => long.TryParse(v.ToString(), out var p) ? p : long.MinValue
            };
        }

        private static DateTimeOffset RowTimeUtc(Dictionary<string, object?> row)
        {
            var s = row.TryGetValue("timeUtc", out var v) ? v as string : null;
            return DateTimeOffset.TryParse(s, out var t) ? t.ToUniversalTime() : DateTimeOffset.MinValue;
        }

        private static bool RowMatchesFilters(
            JsonElement row,
            int? userId,
            string? userEmail,
            string? method,
            string? pathContains,
            int? statusCode,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            string? search)
        {
            if (userId.HasValue && GetInt(row, "userId") != userId.Value)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(userEmail))
            {
                var email = GetString(row, "userEmail");
                if (email == null || !email.Contains(userEmail, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(method))
            {
                var m = GetString(row, "method");
                if (m == null || !string.Equals(m, method, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(pathContains))
            {
                var path = GetString(row, "path");
                if (path == null || !path.Contains(pathContains, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (statusCode.HasValue && GetInt(row, "statusCode") != statusCode.Value)
            {
                return false;
            }

            if (fromUtc.HasValue || toUtc.HasValue)
            {
                var timeUtcStr = GetString(row, "timeUtc");
                if (!DateTimeOffset.TryParse(timeUtcStr, out var timeUtc))
                {
                    return false;
                }

                timeUtc = timeUtc.ToUniversalTime();
                if (fromUtc.HasValue && timeUtc < fromUtc.Value) return false;
                if (toUtc.HasValue && timeUtc > toUtc.Value) return false;
            }

            if (!string.IsNullOrEmpty(search))
            {
                var haystack = string.Join('\n',
                    GetString(row, "path") ?? "",
                    GetString(row, "queryString") ?? "",
                    GetString(row, "requestBody") ?? "",
                    GetString(row, "responseBody") ?? "");
                if (!haystack.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, object?> JsonRowToDictionary(JsonElement row)
        {
            return new Dictionary<string, object?>
            {
                ["timeUtc"] = GetString(row, "timeUtc"),
                ["timeLocal"] = GetString(row, "timeLocal"),
                ["userId"] = GetIntNullable(row, "userId"),
                ["userEmail"] = GetString(row, "userEmail"),
                ["method"] = GetString(row, "method"),
                ["path"] = GetString(row, "path"),
                ["queryString"] = GetString(row, "queryString"),
                ["statusCode"] = GetInt(row, "statusCode"),
                ["durationMs"] = GetLong(row, "durationMs"),
                ["requestSizeBytes"] = GetInt(row, "requestSizeBytes"),
                ["responseSizeBytes"] = GetInt(row, "responseSizeBytes"),
                ["requestBodyTruncated"] = GetBool(row, "requestBodyTruncated"),
                ["responseBodyTruncated"] = GetBool(row, "responseBodyTruncated"),
                // requestBody/responseBody are deliberately NOT included — loaded on demand via /body.
            };
        }

        private static string? GetString(JsonElement row, string name) =>
            row.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

        private static int GetInt(JsonElement row, string name) =>
            row.TryGetProperty(name, out var p) && p.TryGetInt32(out var v) ? v : 0;

        private static int? GetIntNullable(JsonElement row, string name)
        {
            if (!row.TryGetProperty(name, out var p)) return null;
            if (p.ValueKind == JsonValueKind.Null) return null;
            return p.TryGetInt32(out var v) ? v : null;
        }

        private static long GetLong(JsonElement row, string name) =>
            row.TryGetProperty(name, out var p) && p.TryGetInt64(out var v) ? v : 0;

        private static bool GetBool(JsonElement row, string name) =>
            row.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.True;

        private const string HtmlPage =
"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Log Viewer</title>
<style>
  :root { color-scheme: dark; }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: -apple-system, Segoe UI, Roboto, sans-serif; background: #0f1115; color: #d7dae0; }
  header { position: sticky; top: 0; background: #171a21; border-bottom: 1px solid #2a2f3a; padding: 10px 12px; display: flex; flex-wrap: wrap; gap: 8px 12px; align-items: center; }
  header .row { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
  select, input[type=text], input[type=number] { background: #0f1115; color: #d7dae0; border: 1px solid #2a2f3a; border-radius: 6px; padding: 6px 8px; font-size: 13px; }
  input[type=text].search { min-width: 260px; }
  input[type=number] { width: 72px; }
  button { background: #2b6cb0; color: #fff; border: 0; border-radius: 6px; padding: 6px 12px; font-size: 13px; cursor: pointer; }
  button.secondary { background: #2a2f3a; }
  button:hover { filter: brightness(1.1); }
  label { font-size: 12px; color: #9aa3b2; display: inline-flex; gap: 4px; align-items: center; }
  a.dl { color: #63b3ed; font-size: 12px; text-decoration: none; }
  .meta { font-size: 12px; color: #9aa3b2; margin-left: auto; }
  .warn { color: #f6ad55; font-size: 12px; }
  pre { margin: 0; padding: 12px; font-family: ui-monospace, Consolas, monospace; font-size: 12.5px; line-height: 1.45; white-space: pre; overflow: auto; height: calc(100vh - 96px); }
  pre.wrap { white-space: pre-wrap; word-break: break-word; }
  mark { background: #b7791f; color: #fff; border-radius: 2px; }
  .ln { color: #5a6472; user-select: none; -webkit-user-select: none; }
</style>
</head>
<body>
<header>
  <div class="row">
    <select id="file"></select>
    <button id="refresh" class="secondary" title="Refresh">&#x21bb; Refresh</button>
    <label><input type="checkbox" id="auto" /> auto</label>
    <input type="number" id="interval" value="10" min="2" title="seconds" /> <span style="font-size:12px;color:#9aa3b2">s</span>
  </div>
  <div class="row">
    <input type="text" id="search" class="search" placeholder="search (local, filters lines)…" />
    <label><input type="checkbox" id="lineno" checked /> line #</label>
    <label><input type="checkbox" id="wrap" /> wrap</label>
    <label><input type="checkbox" id="newest" /> newest first</label>
    <label>tail <input type="number" id="tail" value="0" min="0" title="0 = all" /></label>
    <label>ctx &plusmn; <input type="number" id="context" value="0" min="0" title="lines before/after each match (only when searching)" /></label>
    <a class="dl" id="download" href="#">download</a>
    <a class="dl" id="httpAudit" href="#">HTTP request audit</a>
    <button id="clear" class="secondary" title="Empty this log file" style="background:#9b2c2c">Clear</button>
  </div>
  <div class="meta" id="meta"></div>
</header>
<pre id="out"></pre>
<script>
  const BASE = location.pathname.replace(/\/+$/, '');
  const $ = (id) => document.getElementById(id);
  let raw = '';
  let sizeMeta = '';
  let timer = null;

  function esc(s) { return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }

  async function loadFiles() {
    try {
      const res = await fetch(BASE + '/files');
      if (!res.ok) throw new Error('HTTP ' + res.status);
      const files = await res.json();
      const cur = $('file').value;
      $('file').innerHTML = '';
      for (const f of files) {
        const kb = (f.size / 1024).toFixed(1);
        const when = new Date(f.modifiedUtc).toLocaleString();
        const opt = document.createElement('option');
        opt.value = f.name;
        opt.textContent = f.name + '  (' + kb + ' KB, ' + when + ')';
        $('file').appendChild(opt);
      }
      if (cur && files.some(f => f.name === cur)) $('file').value = cur;
      if (!$('file').value && files.length) $('file').value = files[0].name;
    } catch (e) {
      $('meta').textContent = 'Failed to list files: ' + e.message;
    }
  }

  async function loadContent() {
    const name = $('file').value;
    if (!name) { raw = ''; render(); return; }
    try {
      const res = await fetch(BASE + '/content?file=' + encodeURIComponent(name));
      if (!res.ok) throw new Error('HTTP ' + res.status);
      const data = await res.json();
      raw = data.text || '';
      $('download').href = BASE + '/content?file=' + encodeURIComponent(name) + '&download=true';
      const kb = (data.size / 1024).toFixed(1);
      sizeMeta = kb + ' KB' + (data.truncated ? ' <span class="warn">(showing last 5 MB)</span>' : '');
      render();
    } catch (e) {
      $('meta').textContent = 'Failed to load: ' + e.message;
    }
  }

  function render() {
    const q = $('search').value.trim().toLowerCase();
    const ctx = Math.max(0, parseInt($('context').value, 10) || 0);
    const all = raw.length ? raw.split(/\r?\n/) : [];
    const total = all.length;

    // Build the display list as {text, isMatch} entries, plus {sep:true} separators between gaps.
    let display;
    let matched = total;

    if (q) {
      const matchSet = new Set();
      for (let i = 0; i < all.length; i++) {
        if (all[i].toLowerCase().includes(q)) matchSet.add(i);
      }
      matched = matchSet.size;

      // Which line indices to show: each match, plus ctx lines before/after (if enabled).
      const show = new Set();
      for (const i of matchSet) {
        const from = Math.max(0, i - ctx);
        const to = Math.min(all.length - 1, i + ctx);
        for (let j = from; j <= to; j++) show.add(j);
      }

      const sorted = [...show].sort((a, b) => a - b);
      display = [];
      let prev = -2;
      for (const idx of sorted) {
        if (ctx > 0 && display.length && idx !== prev + 1) display.push({ sep: true });
        display.push({ text: all[idx], isMatch: matchSet.has(idx), n: idx + 1 });
        prev = idx;
      }
    } else {
      display = all.map((t, i) => ({ text: t, isMatch: false, n: i + 1 }));
    }

    if ($('newest').checked) display = display.slice().reverse();
    const tail = parseInt($('tail').value, 10) || 0;
    if (tail > 0 && display.length > tail) {
      display = $('newest').checked ? display.slice(0, tail) : display.slice(display.length - tail);
    }

    const rx = q ? new RegExp('(' + q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')', 'gi') : null;
    const showLn = $('lineno').checked;
    const width = String(total).length;
    const gutter = (n) => '<span class="ln">' + (n == null ? ' '.repeat(width) : String(n).padStart(width, ' ')) + '</span> ';
    const html = display.map(o => {
      if (o.sep) return (showLn ? gutter(null) : '') + '<span style="color:#4a5568">--</span>';
      let t = esc(o.text);
      if (rx && o.isMatch) t = t.replace(rx, '<mark>$1</mark>');
      return (showLn ? gutter(o.n) : '') + t;
    }).join('\n');

    $('out').innerHTML = html;
    $('out').className = $('wrap').checked ? 'wrap' : '';
    const counts = q ? (matched + ' / ' + total + ' lines' + (ctx > 0 ? ' (+' + ctx + ' ctx)' : '')) : (total + ' lines');
    $('meta').innerHTML = sizeMeta + ' — ' + counts;
  }

  async function clearFile() {
    const name = $('file').value;
    if (!name) return;
    if (!confirm('Clear (empty) "' + name + '"? This cannot be undone.')) return;
    try {
      const res = await fetch(BASE + '/clear?file=' + encodeURIComponent(name), { method: 'POST' });
      if (!res.ok) throw new Error('HTTP ' + res.status);
      await loadContent();
    } catch (e) {
      $('meta').textContent = 'Failed to clear: ' + e.message;
    }
  }

  function setupAuto() {
    if (timer) { clearInterval(timer); timer = null; }
    if ($('auto').checked) {
      const secs = Math.max(2, parseInt($('interval').value, 10) || 10);
      timer = setInterval(() => { loadFiles(); loadContent(); }, secs * 1000);
    }
  }

  $('file').addEventListener('change', loadContent);
  $('refresh').addEventListener('click', () => { loadFiles(); loadContent(); });
  $('clear').addEventListener('click', clearFile);
  $('search').addEventListener('input', render);
  $('wrap').addEventListener('change', render);
  $('newest').addEventListener('change', render);
  $('lineno').addEventListener('change', render);
  $('tail').addEventListener('input', render);
  $('context').addEventListener('input', render);
  $('auto').addEventListener('change', setupAuto);
  $('interval').addEventListener('change', setupAuto);

  $('httpAudit').href = BASE + '/http-request-audit';

  (async () => { await loadFiles(); await loadContent(); })();
</script>
</body>
</html>
""";

        private const string HttpRequestAuditHtmlPage =
"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>HTTP Request Audit</title>
<style>
  :root { color-scheme: dark; }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: -apple-system, Segoe UI, Roboto, sans-serif; background: #0f1115; color: #d7dae0; font-size: 13px; }
  header { position: sticky; top: 0; background: #171a21; border-bottom: 1px solid #2a2f3a; padding: 10px 12px; display: flex; flex-wrap: wrap; gap: 8px 12px; align-items: center; z-index: 2; }
  header .row { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
  select, input[type=text], input[type=number], input[type=datetime-local] { background: #0f1115; color: #d7dae0; border: 1px solid #2a2f3a; border-radius: 6px; padding: 6px 8px; font-size: 13px; }
  input[type=number] { width: 72px; }
  button { background: #2b6cb0; color: #fff; border: 0; border-radius: 6px; padding: 6px 12px; font-size: 13px; cursor: pointer; }
  button.secondary { background: #2a2f3a; }
  button:hover { filter: brightness(1.1); }
  label { font-size: 12px; color: #9aa3b2; }
  a.dl { color: #63b3ed; font-size: 12px; text-decoration: none; }
  .meta { font-size: 12px; color: #9aa3b2; margin-left: auto; }
  .wrap-table { overflow: auto; max-height: calc(100vh - 140px); }
  table { border-collapse: collapse; width: max-content; min-width: 100%; }
  th, td { border: 1px solid #2a2f3a; padding: 6px 8px; vertical-align: top; max-width: 420px; }
  th { background: #171a21; position: sticky; top: 0; z-index: 1; text-align: left; white-space: nowrap; }
  th[title] { cursor: help; text-decoration: underline dotted; text-underline-offset: 3px; }
  td.bodycell { white-space: nowrap; }
  .viewbtn { background: #2a2f3a; color: #63b3ed; border: 1px solid #343b49; border-radius: 6px; padding: 3px 8px; font-size: 11px; cursor: pointer; }
  .viewbtn:hover { filter: brightness(1.25); }
  tr:nth-child(even) { background: #12151c; }
  .trunc { color: #f6ad55; font-size: 11px; }
  .pager { display: flex; gap: 8px; align-items: center; }
  th.sortable { cursor: pointer; user-select: none; }
  th.sortable:hover { color: #fff; }
  th .arr { color: #63b3ed; }
  .totals { font-size: 12px; color: #9aa3b2; }
  .totals b { color: #d7dae0; }
  /* modal */
  #modal { position: fixed; inset: 0; background: rgba(0,0,0,.6); display: none; z-index: 10; align-items: center; justify-content: center; }
  #modal.show { display: flex; }
  #modalCard { background: #12151c; border: 1px solid #2a2f3a; border-radius: 8px; width: min(90vw, 1000px); max-height: 85vh; display: flex; flex-direction: column; }
  #modalHead { display: flex; gap: 8px; align-items: center; padding: 8px 12px; border-bottom: 1px solid #2a2f3a; }
  #modalTitle { font-size: 13px; color: #9aa3b2; margin-right: auto; }
  #modalBody { margin: 0; padding: 12px; overflow: auto; white-space: pre-wrap; word-break: break-word; font-family: ui-monospace, Consolas, monospace; font-size: 12px; line-height: 1.45; }
  /* loader */
  #loader { position: fixed; top: 10px; right: 12px; z-index: 20; display: none; align-items: center; gap: 8px; background: #171a21; border: 1px solid #343b49; border-radius: 20px; padding: 6px 14px 6px 10px; font-size: 12px; color: #cbd2dd; box-shadow: 0 4px 16px rgba(0,0,0,.4); }
  #loader.show { display: flex; }
  #loader .spin { width: 14px; height: 14px; border: 2px solid #343b49; border-top-color: #63b3ed; border-radius: 50%; animation: spin .7s linear infinite; }
  @keyframes spin { to { transform: rotate(360deg); } }
  .wrap-table.busy { opacity: .5; pointer-events: none; transition: opacity .1s; }
</style>
</head>
<body>
<div id="loader"><span class="spin"></span><span id="loaderText">Loading…</span></div>
<header>
  <div class="row">
    <select id="file"></select>
    <button id="refresh" class="secondary">Refresh</button>
    <a class="dl" id="backLogs" href="#">Raw log viewer</a>
  </div>
  <div class="row">
    <label>userId <input type="number" id="userId" min="0" placeholder="any" /></label>
    <label>email <input type="text" id="userEmail" placeholder="contains" /></label>
    <label>method <input type="text" id="method" placeholder="GET" style="width:64px" /></label>
    <label>path <input type="text" id="pathContains" placeholder="contains" /></label>
    <label>status <input type="number" id="statusCode" min="0" placeholder="any" /></label>
  </div>
  <div class="row">
    <label title="Filter by timeUtc (start). Same universal time as the Time (UTC) column.">from (UTC) <input type="datetime-local" id="fromUtc" /></label>
    <label title="Filter by timeUtc (end). Same universal time as the Time (UTC) column.">to (UTC) <input type="datetime-local" id="toUtc" /></label>
    <label>search <input type="text" id="search" placeholder="body/path…" /></label>
    <button id="apply">Apply filters</button>
  </div>
  <div class="row">
    <label title="Same as raw log viewer: show most recent requests first (by timeUtc)."><input type="checkbox" id="newest" checked /> newest first</label>
    <label title="Limit to the last N matching rows (0 = all). With newest first, keeps the N most recent.">tail <input type="number" id="tail" value="0" min="0" title="0 = all" /></label>
  </div>
  <div class="row pager">
    <button id="prev" class="secondary">Prev</button>
    <span id="pageInfo"></span>
    <button id="next" class="secondary">Next</button>
    <label>page size <input type="number" id="pageSize" value="50" min="1" max="200" /></label>
    <span class="totals" id="totals"></span>
  </div>
  <div class="meta" id="meta"></div>
</header>
<div class="wrap-table">
  <table id="grid">
    <thead id="head"></thead>
    <tbody id="body"></tbody>
  </table>
</div>
<div id="modal">
  <div id="modalCard">
    <div id="modalHead">
      <span id="modalTitle"></span>
      <button id="modalCopy" class="secondary">Copy</button>
      <button id="modalClose">Close</button>
    </div>
    <pre id="modalBody"></pre>
  </div>
</div>
<script>
  const BASE = location.pathname.replace(/\/+$/, '');
  const LOG_BASE = BASE.replace(/\/http-request-audit$/, '');
  const $ = (id) => document.getElementById(id);
  let page = 1;
  let sortBy = '';      // '' = default (time, controlled by "newest first")
  let sortDir = 'desc';
  let currentRows = []; // the rows for the current page (full values, for the modal)

  function esc(s) {
    if (s == null) return '';
    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  // Loader shown during fetches so the user can tell a filter/sort is in flight vs already applied.
  let busyCount = 0;
  function showLoader(text) {
    busyCount++;
    $('loaderText').textContent = text || 'Loading…';
    $('loader').classList.add('show');
    document.querySelector('.wrap-table').classList.add('busy');
  }
  function hideLoader() {
    busyCount = Math.max(0, busyCount - 1);
    if (busyCount === 0) {
      $('loader').classList.remove('show');
      document.querySelector('.wrap-table').classList.remove('busy');
    }
  }

  function fmtBytes(n) {
    n = Number(n) || 0;
    if (n < 1024) return n + ' B';
    if (n < 1024 * 1024) return (n / 1024).toFixed(1) + ' KB';
    return (n / 1024 / 1024).toFixed(2) + ' MB';
  }

  function attrEsc(s) {
    return String(s).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;');
  }

  const COL_META = {
    timeUtc: {
      label: 'Time (UTC)',
      title: 'Exact request time in Coordinated Universal Time (Z). Use this to convert to any timezone.'
    },
    timeLocal: {
      label: 'Time (Pakistan)',
      title: 'Same moment as Time (UTC), shown in Pakistan Standard Time (Diagnostics:AuditDisplayTimeZone). Handy for local support hours.'
    },
    userId: { label: 'userId', title: 'Authenticated user id from JWT; empty if anonymous.' },
    userEmail: { label: 'userEmail', title: 'Email claim from JWT when the request was authenticated.' },
    method: { label: 'method', title: 'HTTP method (GET, POST, etc.).' },
    path: { label: 'path', title: 'Request path without query string.' },
    queryString: { label: 'queryString', title: 'Query string including leading ? if present.' },
    statusCode: { label: 'statusCode', title: 'HTTP response status code.' },
    durationMs: { label: 'durationMs', title: 'Time from request start until response finished, in milliseconds.' },
    requestSizeBytes: { label: 'requestSizeBytes', title: 'Raw request body size in bytes.' },
    responseSizeBytes: { label: 'responseSizeBytes', title: 'Raw response body size in bytes.' },
    requestBodyTruncated: { label: 'requestBodyTruncated', title: 'True if request body was capped by HttpRequestAuditMaxBodyChars in config.' },
    responseBodyTruncated: { label: 'responseBodyTruncated', title: 'True if response body was capped by HttpRequestAuditMaxBodyChars in config.' },
    requestBody: { label: 'requestBody', title: 'Logged request body (UTF-8 text or base64 for binary).' },
    responseBody: { label: 'responseBody', title: 'Logged response body (UTF-8 text or base64 for binary).' }
  };

  function columnHeader(colKey) {
    const meta = COL_META[colKey] || { label: colKey, title: '' };
    const arrow = sortBy === colKey ? ' <span class="arr">' + (sortDir === 'desc' ? '▼' : '▲') + '</span>' : '';
    const t = meta.title ? ' title="' + attrEsc(meta.title) + ' — click to sort"' : ' title="click to sort"';
    return '<th class="sortable" data-col="' + attrEsc(colKey) + '"' + t + '>' + esc(meta.label) + arrow + '</th>';
  }

  async function loadFiles() {
    const res = await fetch(LOG_BASE + '/files');
    if (!res.ok) throw new Error('HTTP ' + res.status);
    const files = await res.json();
    const audit = files.filter(f => f.name.startsWith('http-request-audit-') && f.name.endsWith('.log'));
    const cur = $('file').value;
    $('file').innerHTML = '';
    for (const f of audit) {
      const opt = document.createElement('option');
      opt.value = f.name;
      const kb = (f.size / 1024).toFixed(1);
      opt.textContent = f.name + ' (' + kb + ' KB)';
      $('file').appendChild(opt);
    }
    if (cur && audit.some(f => f.name === cur)) $('file').value = cur;
    if (!$('file').value && audit.length) $('file').value = audit[0].name;
  }

  function queryParams() {
    const p = new URLSearchParams();
    p.set('file', $('file').value);
    p.set('page', String(page));
    p.set('pageSize', String(Math.min(200, Math.max(1, parseInt($('pageSize').value, 10) || 50))));
    const uid = $('userId').value.trim();
    if (uid) p.set('userId', uid);
    const em = $('userEmail').value.trim();
    if (em) p.set('userEmail', em);
    const m = $('method').value.trim();
    if (m) p.set('method', m);
    const path = $('pathContains').value.trim();
    if (path) p.set('pathContains', path);
    const st = $('statusCode').value.trim();
    if (st) p.set('statusCode', st);
    const from = $('fromUtc').value;
    if (from) p.set('fromUtc', from + ':00.000Z');
    const to = $('toUtc').value;
    if (to) p.set('toUtc', to + ':00.000Z');
    const q = $('search').value.trim();
    if (q) p.set('search', q);
    p.set('newestFirst', $('newest').checked ? 'true' : 'false');
    const tail = parseInt($('tail').value, 10) || 0;
    if (tail > 0) p.set('tail', String(tail));
    if (sortBy) { p.set('sortBy', sortBy); p.set('sortDir', sortDir); }
    return p;
  }

  async function loadRows() {
    const name = $('file').value;
    if (!name) {
      $('meta').textContent = 'No audit log files yet.';
      $('head').innerHTML = '';
      $('body').innerHTML = '';
      return;
    }
    showLoader($('search').value ? 'Searching…' : 'Filtering…');
    try {
      const res = await fetch(BASE + '/rows?' + queryParams().toString());
      if (!res.ok) throw new Error('HTTP ' + res.status);
      const data = await res.json();
      const cols = data.columns || [];
      currentRows = data.rows || [];
      $('head').innerHTML = '<tr>' + cols.map(c => columnHeader(c)).join('') + '</tr>';
      $('body').innerHTML = currentRows.map((row) => {
        return '<tr>' + cols.map(c => {
          if (c === 'requestBody' || c === 'responseBody') {
            // Bodies aren't shipped with the grid — show a light "view" button that loads on click.
            const which = c === 'requestBody' ? 'request' : 'response';
            const size = Number(row[which + 'SizeBytes']) || 0;
            if (size <= 0) return '<td class="bodycell">—</td>';
            const truncKey = c === 'requestBody' ? 'requestBodyTruncated' : 'responseBodyTruncated';
            const trunc = row[truncKey] ? ' <span class="trunc">trunc</span>' : '';
            return '<td class="bodycell"><button class="viewbtn" data-line="' + row.__line + '" data-which="' + which + '">view (' + fmtBytes(size) + ')</button>' + trunc + '</td>';
          }
          return '<td>' + esc(row[c]) + '</td>';
        }).join('') + '</tr>';
      }).join('');
      const pages = Math.max(1, Math.ceil((data.total || 0) / (data.pageSize || 50)));
      $('pageInfo').textContent = 'Page ' + data.page + ' / ' + pages + ' (' + data.total + ' rows)';
      $('totals').innerHTML = 'totals (filtered): req <b>' + fmtBytes(data.totalRequestBytes) + '</b> · resp <b>' + fmtBytes(data.totalResponseBytes) + '</b>';
      $('meta').textContent = data.file;
      page = data.page;
    } catch (e) {
      $('meta').textContent = 'Failed: ' + e.message;
    } finally {
      hideLoader();
    }
  }

  $('backLogs').href = LOG_BASE;
  $('refresh').addEventListener('click', async () => { await loadFiles(); await loadRows(); });
  $('apply').addEventListener('click', () => { page = 1; loadRows(); });
  $('file').addEventListener('change', () => { page = 1; loadRows(); });
  $('prev').addEventListener('click', () => { if (page > 1) { page--; loadRows(); } });
  $('next').addEventListener('click', () => { page++; loadRows(); });
  $('pageSize').addEventListener('change', () => { page = 1; loadRows(); });
  $('newest').addEventListener('change', () => { page = 1; loadRows(); });
  $('tail').addEventListener('input', () => { page = 1; loadRows(); });

  // Sort by clicking a column header (toggles asc/desc on the same column).
  $('head').addEventListener('click', (e) => {
    const th = e.target.closest('th.sortable');
    if (!th) return;
    const col = th.getAttribute('data-col');
    if (sortBy === col) {
      sortDir = sortDir === 'desc' ? 'asc' : 'desc';
    } else {
      sortBy = col;
      sortDir = 'desc';
    }
    page = 1;
    loadRows();
  });

  // Click a "view" button to fetch that one body on demand and show it in the modal.
  $('body').addEventListener('click', async (e) => {
    const btn = e.target.closest('.viewbtn');
    if (!btn) return;
    const lineNo = btn.getAttribute('data-line');
    const which = btn.getAttribute('data-which');
    openModal(which + ' body  —  line ' + lineNo, 'Loading…');
    showLoader('Loading body…');
    try {
      const p = new URLSearchParams({ file: $('file').value, line: lineNo, which });
      const res = await fetch(BASE + '/body?' + p.toString());
      if (!res.ok) throw new Error('HTTP ' + res.status);
      const data = await res.json();
      let text = data.body == null ? '(empty)' : String(data.body);
      if (data.truncated) text += '\n\n--- (truncated in file) ---';
      $('modalBody').textContent = text;
    } catch (err) {
      $('modalBody').textContent = 'Failed to load body: ' + err.message;
    } finally {
      hideLoader();
    }
  });

  function openModal(title, text) {
    $('modalTitle').textContent = title;
    $('modalBody').textContent = text == null ? '' : String(text);
    $('modal').classList.add('show');
  }
  function closeModal() { $('modal').classList.remove('show'); }
  $('modalClose').addEventListener('click', closeModal);
  $('modal').addEventListener('click', (e) => { if (e.target.id === 'modal') closeModal(); });
  document.addEventListener('keydown', (e) => { if (e.key === 'Escape') closeModal(); });
  $('modalCopy').addEventListener('click', async () => {
    try {
      await navigator.clipboard.writeText($('modalBody').textContent || '');
      $('modalCopy').textContent = 'Copied';
      setTimeout(() => { $('modalCopy').textContent = 'Copy'; }, 1200);
    } catch (e) { /* clipboard blocked */ }
  });

  (async () => { await loadFiles(); await loadRows(); })();
</script>
</body>
</html>
""";
    }
}
