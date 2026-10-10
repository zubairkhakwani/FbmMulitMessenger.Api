using FBMMultiMessenger.Buisness.Helpers;
using Microsoft.AspNetCore.Mvc;
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
            [FromQuery] int tail = 0)
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

            var matched = new List<Dictionary<string, object?>>();

            using (var fs = System.IO.File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
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

                    matched.Add(JsonRowToDictionary(row));
                }
            }

            IEnumerable<Dictionary<string, object?>> ordered = newestFirst
                ? matched.OrderByDescending(r => RowTimeUtc(r))
                : matched.OrderBy(r => RowTimeUtc(r));

            var list = ordered.ToList();
            tail = Math.Max(0, tail);
            if (tail > 0 && list.Count > tail)
            {
                list = newestFirst
                    ? list.Take(tail).ToList()
                    : list.Skip(list.Count - tail).Take(tail).ToList();
            }

            var total = list.Count;
            var skip = (page - 1) * pageSize;
            var pageRows = list.Skip(skip).Take(pageSize).ToList();

            return Ok(new { columns, rows = pageRows, total, page, pageSize, file = safeName, newestFirst, tail });
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
                ["requestBody"] = GetString(row, "requestBody"),
                ["responseBody"] = GetString(row, "responseBody"),
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
  td.body { font-family: ui-monospace, Consolas, monospace; font-size: 11px; white-space: pre-wrap; word-break: break-word; }
  tr:nth-child(even) { background: #12151c; }
  .trunc { color: #f6ad55; font-size: 11px; }
  .pager { display: flex; gap: 8px; align-items: center; }
</style>
</head>
<body>
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
  </div>
  <div class="meta" id="meta"></div>
</header>
<div class="wrap-table">
  <table id="grid">
    <thead id="head"></thead>
    <tbody id="body"></tbody>
  </table>
</div>
<script>
  const BASE = location.pathname.replace(/\/+$/, '');
  const LOG_BASE = BASE.replace(/\/http-request-audit$/, '');
  const $ = (id) => document.getElementById(id);
  let page = 1;

  function esc(s) {
    if (s == null) return '';
    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
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
    if (meta.title) {
      return '<th title="' + attrEsc(meta.title) + '">' + esc(meta.label) + '</th>';
    }
    return '<th>' + esc(meta.label) + '</th>';
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
    try {
      const res = await fetch(BASE + '/rows?' + queryParams().toString());
      if (!res.ok) throw new Error('HTTP ' + res.status);
      const data = await res.json();
      const cols = data.columns || [];
      $('head').innerHTML = '<tr>' + cols.map(c => columnHeader(c)).join('') + '</tr>';
      $('body').innerHTML = (data.rows || []).map(row => {
        return '<tr>' + cols.map(c => {
          let v = row[c];
          if (c === 'requestBody' || c === 'responseBody') {
            let cell = esc(v);
            const truncKey = c === 'requestBody' ? 'requestBodyTruncated' : 'responseBodyTruncated';
            if (row[truncKey]) cell += ' <span class="trunc">(truncated in file)</span>';
            return '<td class="body">' + cell + '</td>';
          }
          return '<td>' + esc(v) + '</td>';
        }).join('') + '</tr>';
      }).join('');
      const pages = Math.max(1, Math.ceil((data.total || 0) / (data.pageSize || 50)));
      $('pageInfo').textContent = 'Page ' + data.page + ' / ' + pages + ' (' + data.total + ' rows)';
      $('meta').textContent = data.file;
      page = data.page;
    } catch (e) {
      $('meta').textContent = 'Failed: ' + e.message;
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

  (async () => { await loadFiles(); await loadRows(); })();
</script>
</body>
</html>
""";
    }
}
