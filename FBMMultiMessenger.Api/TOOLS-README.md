# Tools folder — manual step after publishing

The `Tools/JsObfuscator` folder contains the JavaScript obfuscator CLI
(`javascript-obfuscator.cmd` + its `node_modules`) and a bundled `node.exe`.
It is used at **runtime** to obfuscate the browser extension files before they
are encrypted and served from the extension endpoints.

## Why it is not published automatically

The obfuscator's `node_modules` has very deeply nested paths. When MSBuild copies
them into the publish output (`bin\Release\net9.0\publish\...`) the full path
exceeds the Windows 260-character `MAX_PATH` limit and publishing fails with:

```
Could not copy "...\node_modules\...\*.d.ts.map" ... Exceeded retry count of 10. Failed.
```

To avoid this, the `Tools` folder is copied to the local build output (for
development) but is **excluded from publish** (`CopyToPublishDirectory=Never`
in `FBMMultiMessenger.Api.csproj`).

## Required manual step

**After every publish**, copy the `Tools` folder into the root of the published
output (next to `FBMMultiMessenger.Api.dll`), so the final layout is:

```
publish/
├── FBMMultiMessenger.Api.dll
├── BrowserExtension/
├── ProxyExtension/
└── Tools/
    └── JsObfuscator/
        ├── javascript-obfuscator.cmd
        ├── node.exe
        └── node_modules/
```

Example (PowerShell, run from the project folder):

```powershell
Copy-Item -Recurse -Force "Tools" "bin\Release\net9.0\publish\Tools"
```

> Tip: to avoid the `MAX_PATH` error when copying manually, either enable Windows
> long-path support, or copy with a tool that handles long paths (e.g. `robocopy`).

## Runtime requirement

The app runs `Tools\JsObfuscator\javascript-obfuscator.cmd`, which uses the
bundled `node.exe` next to it — so **no Node.js installation is required on the
server**. The bundled `node.exe` is Windows x64; the host must be Windows x64.
