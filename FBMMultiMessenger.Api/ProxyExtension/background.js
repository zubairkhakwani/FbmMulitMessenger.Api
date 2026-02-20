console.log("Proxy Background script running.");

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

function clearProxy() {
    return new Promise((resolve) => {
        chrome.proxy.settings.set(
            { value: { mode: "direct" }, scope: "regular" },
            () => {
                console.log("Proxy removed for diagnosis.");
                resolve();
            }
        );
    });
}

function restoreProxy() {
    return new Promise((resolve) => {
        chrome.proxy.settings.set(
            {
                value: {
                    mode: "fixed_servers",
                    rules: {
                        singleProxy: {
                            host: proxyHost, // your proxy host
                            port: proxyPort  // your proxy port
                        }
                    }
                },
                scope: "regular"
            },
            () => {
                console.log("Proxy restored.");
                resolve();
            }
        );
    });
}


async function checkUsingHttpClient() {
    try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 15000);
        await fetch("https://www.google.com", { signal: controller.signal, mode: 'no-cors' });
        clearTimeout(timeoutId);
        return true;
    } catch (err) {
        return false;
    }
}
async function checkInternetAndProxy() {
    // Step 1: Check with proxy
    let withProxy = await checkUsingHttpClient();

    if (withProxy) {
        console.log("Internet & Proxy are both working.");
        return;
    }

    // Step 2: Something failed — temporarily remove proxy and check again
    console.log("Initial check failed, diagnosing...");
    await clearProxy();

    let withoutProxy = await checkUsingHttpClient();

    if (withoutProxy) {
        console.error("Internet is UP but Proxy is DOWN or misconfigured.");
        // optionally: notify user, retry proxy setup, etc.
    } else {
        console.log("Internet is DOWN.");
    }

    // Step 3: Restore proxy after diagnosis
    //await restoreProxy();
}


//  Run once on startup
setTimeout(() => {
    checkInternetAndProxy();
}, 1100);

//repeat every 1 minute 
setInterval(() => {
    checkInternetAndProxy();
}, 60 * 1000);


chrome.runtime.onInstalled.addListener(() => {
    chrome.alarms.create('keepAlive', { periodInMinutes: 0.2 });
});

chrome.alarms.onAlarm.addListener((alarm) => {
    if (alarm.name === 'keepAlive') {
        console.log('Proxy Service worker kept alive.');
    }
});


