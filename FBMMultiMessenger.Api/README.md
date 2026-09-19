check Browser Extension after obfuscation on ixbrwoser, sometimes it was not working on ixbrowser, last i tested was because
inject.js has debugger; statement, it does not work on ixbrowser.

so in inject.js has any debugger statement, please remove it for production.