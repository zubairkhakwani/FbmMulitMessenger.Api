using System.Text.Json;

namespace FBMMultiMessenger.Buisness.Helpers
{
    /// <summary>
    /// Builds the encrypted extension payload from obfuscated files and caches it in memory,
    /// so obfuscation runs only on first request / on update, not on every request.
    /// </summary>
    public class ExtensionContentCache
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private string? _encrypted;

        /// <summary>Returns the cached payload, building it on first use.</summary>
        public async Task<string> GetAsync(string? extensionVersion, AesEncryptionHelper aes)
        {
            if (_encrypted != null) return _encrypted;

            await _gate.WaitAsync();
            try
            {
                return _encrypted ??= await BuildAsync(extensionVersion, aes);
            }
            finally { _gate.Release(); }
        }

        /// <summary>Re-obfuscates and replaces the cached payload (called on extension update).</summary>
        public async Task<string> RebuildAsync(string? extensionVersion, AesEncryptionHelper aes)
        {
            await _gate.WaitAsync();
            try
            {
                return _encrypted = await BuildAsync(extensionVersion, aes);
            }
            finally { _gate.Release(); }
        }

        private static async Task<string> BuildAsync(string? extensionVersion, AesEncryptionHelper aes)
        {
            string baseDir = AppContext.BaseDirectory;
            string extensionFolder = Path.Combine(baseDir, "BrowserExtension");
            string proxyExtensionFolder = Path.Combine(baseDir, "ProxyExtension");

            if (!Directory.Exists(extensionFolder))
                throw new DirectoryNotFoundException($"BrowserExtension folder not found at: {extensionFolder}");

            if (!Directory.Exists(proxyExtensionFolder))
                throw new DirectoryNotFoundException($"ProxyExtension folder not found at: {proxyExtensionFolder}");

            string obfExtensionFolder = Path.Combine(baseDir, "ObfuscatedExtension");
            string obfuscatorPath = Path.Combine(baseDir, "Tools", "JsObfuscator", "javascript-obfuscator.cmd");

            // Only the browser extension JavaScript files get obfuscated; manifest/html/vendor lib
            // and the whole proxy extension are passed through as-is (proxy stays un-obfuscated).
            await JsObfuscator.ObfuscateFilesAsync(
                new[]
                {
                    Path.Combine(extensionFolder, "background.js"),
                    Path.Combine(extensionFolder, "inject.js"),
                    Path.Combine(extensionFolder, "content.js"),
                    Path.Combine(extensionFolder, "popup.js"),
                },
                obfuscatorPath,
                obfExtensionFolder);

            var payload = new
            {
                ExtensionVersion = extensionVersion,
                BackgroundJs = await File.ReadAllTextAsync(Path.Combine(obfExtensionFolder, "background.js")),
                InjectJs = await File.ReadAllTextAsync(Path.Combine(obfExtensionFolder, "inject.js")),
                ContentJs = await File.ReadAllTextAsync(Path.Combine(obfExtensionFolder, "content.js")),
                ManifestJson = await File.ReadAllTextAsync(Path.Combine(extensionFolder, "manifest.json")),
                SignalRPackage = await File.ReadAllTextAsync(Path.Combine(extensionFolder, "signalR.min.js")),
                PopupHtml = await File.ReadAllTextAsync(Path.Combine(extensionFolder, "popup.html")),
                PopupJs = await File.ReadAllTextAsync(Path.Combine(obfExtensionFolder, "popup.js")),
                ProxyBackgroundJs = await File.ReadAllTextAsync(Path.Combine(proxyExtensionFolder, "background.js")),
                ProxyManifestJson = await File.ReadAllTextAsync(Path.Combine(proxyExtensionFolder, "manifest.json"))
            };

            return aes.Encrypt(JsonSerializer.Serialize(payload));
        }
    }
}
