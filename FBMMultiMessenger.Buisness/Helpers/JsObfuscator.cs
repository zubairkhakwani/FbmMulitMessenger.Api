using System.Diagnostics;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public class JsObfuscator
    {
        /// <summary>
        /// Obfuscates a list of JavaScript files using javascript-obfuscator CLI
        /// with the configured settings, writing results to the specified output folder.
        /// </summary>
        /// <param name="inputFiles">List of paths to .js files to obfuscate</param>
        /// <param name="outputFolder">Destination folder for obfuscated files</param>
        /// <param name="nodePath">Path to node executable (default: "node")</param>
        /// <param name="obfuscatorPath">Path to javascript-obfuscator CLI (default: "javascript-obfuscator")</param>
        public static async Task ObfuscateFilesAsync(
            IEnumerable<string> inputFiles,
            string obfuscatorPath,
            string outputFolder,
            string nodePath = "node")
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            foreach (var inputFile in inputFiles)
            {
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine($"[SKIP] File not found: {inputFile}");
                    continue;
                }

                var fileName = Path.GetFileName(inputFile);
                var outputFile = Path.Combine(outputFolder, fileName);

                var args = BuildObfuscatorArgs(inputFile, outputFile);

                Console.WriteLine($"[OBFUSCATING] {fileName}");

                // Call the .cmd directly — NOT via node
                var exitCode = await RunProcessAsync(obfuscatorPath, args);

                if (exitCode == 0)
                {
                    Console.WriteLine($"[OK] Output: {outputFile}");
                }
                else
                {
                    Console.WriteLine($"[ERROR] Exit code {exitCode} for: {inputFile}");
                }
            }
        }

        /// <summary>
        /// Builds the CLI argument string from the screenshot settings.
        /// </summary>
        private static string BuildObfuscatorArgs(string inputFile, string outputFile)
        {
            // All settings mapped from the screenshot
            var options = new List<string>
        {
            // ── Basic Options ──────────────────────────────────────────────
            "--target",                     "browser",
            "--seed",                       "0",

            // ── Strings ────────────────────────────────────────────────────
            "--string-array",               "true",
            "--string-array-rotate",        "true",
            "--string-array-shuffle",       "true",
            "--string-array-index-shift",   "true",
            "--string-array-threshold",     "0.75",
            "--string-array-indexes-type",  "hexadecimal-number",
            "--string-array-encoding",      "none",
            "--string-array-wrappers-count","1",
            "--string-array-wrappers-type", "variable",
            "--string-array-wrappers-chained-calls", "true",
            "--string-array-calls-transform","false",
            "--split-strings",              "false",

            // ── Identifiers ────────────────────────────────────────────────
            "--identifier-names-generator", "mangled",
            "--rename-globals",             "false",
            "--rename-properties",          "false",

            // ── Code Transformations ───────────────────────────────────────
            "--compact",                    "true",
            "--simplify",                   "true",
            "--transform-object-keys",      "true",
            "--numbers-to-expressions",     "true",
            "--control-flow-flattening",    "true",
            "--control-flow-flattening-threshold", "0.4",
            "--dead-code-injection",        "true",
            "--dead-code-injection-threshold", "0.4",

            // ── Protection (OFF in screenshot) ─────────────────────────────
            "--self-defending",             "false",
            "--debug-protection",           "false",
            "--disable-console-output",     "false",

            // ── Source Map (OFF) ───────────────────────────────────────────
            "--source-map",                 "false",
        };

            var optionsStr = string.Join(" ", options);

            // Call pattern: node javascript-obfuscator <input> --output <output> [options]
            return $"\"{inputFile}\" --output \"{outputFile}\" {optionsStr}";
        }

        private static async Task<int> RunProcessAsync(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = false, // Must be false when UseShellExecute=true
                RedirectStandardError = false, // Must be false when UseShellExecute=true
                UseShellExecute = true,  // Required for .cmd on Windows
                CreateNoWindow = true,
            };

            var process = Process.Start(psi);

            var logs = new List<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logs.Add("[PROJECT-OUT] " + e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logs.Add("[PROJECT-ERR] " + e.Data);
                }
            };

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine(string.Join("\n", logs));
            }


            return process.ExitCode;
        }
    }
}
