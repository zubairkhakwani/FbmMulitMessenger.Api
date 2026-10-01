using FBMMultiMessenger.Data.Database.DbModels;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace FBMMultiMessenger.Buisness.Helpers
{
    /// <summary>
    /// Builds a loadable (unpacked) browser-extension ZIP for public website download and caches it in
    /// memory. Unlike <see cref="ExtensionContentCache"/> (which AES-encrypts the payload for FBM Robo to
    /// decrypt/inject/install), this produces raw files a user can install manually via
    /// chrome://extensions → Developer mode → Load unpacked. The API URL is pointed at the public host so
    /// the download works standalone (the user authenticates with an API key through the popup).
    /// </summary>
    public class ExtensionZipCache
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private byte[]? _zip;

        // JS files that get obfuscated (same set as the Robo payload).
        private static readonly string[] JsFiles = { "background.js", "inject.js", "content.js", "popup.js" };

        // Files shipped as-is (not obfuscated).
        private static readonly string[] PassthroughFiles = { "manifest.json", "popup.html", "signalR.min.js" };

        /// <summary>Returns the cached ZIP, building it on first use.</summary>
        public async Task<byte[]> GetAsync(string publicApiUrl)
        {
            if (_zip != null) return _zip;

            await _gate.WaitAsync();
            try
            {
                return _zip ??= await BuildAsync(publicApiUrl);
            }
            finally { _gate.Release(); }
        }

        /// <summary>Rebuilds and replaces the cached ZIP (called on extension update).</summary>
        public async Task<byte[]> RebuildAsync(string publicApiUrl)
        {
            await _gate.WaitAsync();
            try
            {
                return _zip = await BuildAsync(publicApiUrl);
            }
            finally { _gate.Release(); }
        }

        private static async Task<byte[]> BuildAsync(string publicApiUrl)
        {
            publicApiUrl = publicApiUrl.TrimEnd('/');

            string baseDir = AppContext.BaseDirectory;
            string extensionFolder = Path.Combine(baseDir, "BrowserExtension");

            if (!Directory.Exists(extensionFolder))
                throw new DirectoryNotFoundException($"BrowserExtension folder not found at: {extensionFolder}");

            string stagingFolder = Path.Combine(baseDir, "PublicExtensionStaging");
            string obfFolder = Path.Combine(baseDir, "PublicExtensionObf");
            Directory.CreateDirectory(stagingFolder);

            // Stage the JS sources, pointing the API URL at the public host BEFORE obfuscation. The
            // obfuscator mangles string literals, so the URL swap must happen on the raw source. The
            // regex rewrites every "var remoteApiUrl = "...";" occurrence (the active one and the
            // commented alternative) so the build is correct regardless of which is uncommented in source.
            foreach (var file in JsFiles)
            {
                var content = await File.ReadAllTextAsync(Path.Combine(extensionFolder, file));

                if (file == "background.js")
                {
                    content = content
                        .Replace("%%FBM_AUTO_OPEN_MESSENGER%%", "false")
                        .Replace("%%FBM_ROBO_API_KEY%%", "")
                        .Replace("https://localhost:7095", publicApiUrl);
                }

                if(file == "inject.js")
                {
                    content = content.Replace("%%FBM_AUTO_OPEN_MESSENGER%%", "false");
                }

                await File.WriteAllTextAsync(Path.Combine(stagingFolder, file), content);
            }

            string obfuscatorPath = Path.Combine(baseDir, "Tools", "JsObfuscator", "javascript-obfuscator.cmd");

            // Obfuscate the staged (URL-swapped) files. If the obfuscator tooling isn't available on the
            // host, we fall back to the staged files below so the download is still valid/installable.
            try
            {
                await JsObfuscator.ObfuscateFilesAsync(
                    JsFiles.Select(f => Path.Combine(stagingFolder, f)),
                    obfuscatorPath,
                    obfFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtensionZipCache] Obfuscation failed, packaging staged files as-is: {ex.Message}");
            }

            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var file in JsFiles)
                {
                    // Prefer the obfuscated output; fall back to the staged (URL-swapped) source.
                    var obfPath = Path.Combine(obfFolder, file);
                    var source = File.Exists(obfPath) ? obfPath : Path.Combine(stagingFolder, file);
                    await AddFileAsync(zip, source, file);
                }

                foreach (var file in PassthroughFiles)
                {
                    await AddFileAsync(zip, Path.Combine(extensionFolder, file), file);
                }
            }

            return ms.ToArray();
        }

        private static async Task AddFileAsync(ZipArchive zip, string sourcePath, string entryName)
        {
            if (!File.Exists(sourcePath))
            {
                Console.WriteLine($"[ExtensionZipCache] Missing file, skipped: {sourcePath}");
                return;
            }

            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(sourcePath);
            await fileStream.CopyToAsync(entryStream);
        }
    }
}
