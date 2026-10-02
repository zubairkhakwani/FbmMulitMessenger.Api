using FBMMultiMessenger.Buisness.Helpers;
using Microsoft.AspNetCore.Mvc;

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

  (async () => { await loadFiles(); await loadContent(); })();
</script>
</body>
</html>
""";
    }
}
