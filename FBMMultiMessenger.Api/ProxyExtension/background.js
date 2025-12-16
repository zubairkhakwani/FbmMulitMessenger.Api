console.log("Background script running");

if (userName && password) {
    chrome.webRequest.onAuthRequired.addListener(function (details, callbackFn) {
        callbackFn({
            authCredentials: { username: userName, password: password }
        });
    }, { urls: ["<all_urls>"] }, ['asyncBlocking']);
}

chrome.windows.onRemoved.addListener(function (windowId) {
    chrome.windows.getAll({}, function (windows) {
        if (windows.length === 0) {
            clearProxy();
        }
    });
});

chrome.tabs.onRemoved.addListener(function (tabId, removeInfo) {
    chrome.windows.getAll({}, function (windows) {
        if (windows.length === 0) {
            clearProxy(); // Call the function to clear proxy settings when all windows are closed
        }
    });
});

function clearProxy()
{
    chrome.proxy.settings.clear({ scope: 'regular' });

    chrome.proxy.settings.set(
        {
            value: {
                mode: "direct"
            },
            scope: "regular"
        },
        function () {
            console.log("Proxy settings cleaned.");
        }
    );
}