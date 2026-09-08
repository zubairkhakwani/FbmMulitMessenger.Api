// Import SignalR library

importScripts("./signalr.min.js");

console.log('Scrapping Background script running.');

//var remoteApiUrl = "https://api.fbmmessenger.com";
var remoteApiUrl = "https://localhost:7095";
var remoteAPISignalRUrl = `${remoteApiUrl}/chathub`;
var accountId = null;
var apiUserId = null;

// Add near the top with other variables
var authToken = null;

let signalRConnection = null;
let isConnected = false;
let reconnectTimeout = null;
let isReconnecting = false;
let isManuallyStopped = false; // NEW — prevents auto reconnect when we intentionally disconnect

let failedRequestQueue = [];

function enqueueFailedRequest(key, payload) {
    failedRequestQueue.push({ key, payload, timestamp: Date.now() });
    console.log(`Request queued for retry. Queue size: ${failedRequestQueue.length}`);
}

async function retryFailedRequests() {
    if (failedRequestQueue.length === 0) return;

    console.log(`Retrying ${failedRequestQueue.length} failed requests...`);
    const queue = [...failedRequestQueue];
    failedRequestQueue = []; // clear before retry to avoid duplicates

    for (const request of queue) {
        try {
            if (request.key === 'sendRawChunkToApi') {
                await apiFetch(`${remoteApiUrl}/api/sync`, {
                    method: 'POST',
                    body: JSON.stringify(request.payload),
                });
            }
            else if (request.key === 'syncListingDetail') {
                await apiFetch(`${remoteApiUrl}/api/sync/listing-info`, {
                    method: 'POST',
                    body: JSON.stringify(request.payload),
                });
            }
            else if (request.key === 'registerAccount') {
                const registered = await registerAccount(request.payload.fbAccountId);
                if (!registered) {
                    throw new Error('Registration still failing');
                }

                await apiFetch(`${remoteApiUrl}/api/account/${accountId}/status`, {
                    method: 'PUT',
                    body: JSON.stringify({
                        ...request.payload,  // use original payload as-is from inject.js
                        accountId,
                    }),
                });

                // Once registered, connect SignalR
                await connectSignalR();
            }
            console.log(`Retry succeeded for: ${request.key}`);
        } catch (err) {
            console.error(`Retry failed again for ${request.key}, re-queuing...`);
            failedRequestQueue.push(request); // re-queue if still failing
        }
    }
}

async function initializeSignalR() {
    // Don't reinitialize if already built
    if (signalRConnection) return true;

    try {
        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${remoteAPISignalRUrl}`, {
                withCredentials: true,
                //accessTokenFactory: () => authToken  // attach bearer token
            })
            .withAutomaticReconnect([0, 500])
            .configureLogging(signalR.LogLevel.None)
            .build();

        signalRConnection.onreconnecting(() => {
            console.log("SignalR reconnecting...");
            isConnected = false;
            isReconnecting = true;
            notifyPopupStatusChange();
        });

        signalRConnection.onreconnected(() => {
            console.log("SignalR reconnected");
            isConnected = true;
            isReconnecting = false;
            registerExtensionUser().catch(err => console.error("Failed to register after reconnect:", err));
            notifyPopupStatusChange();
            retryFailedRequests();
        });

        signalRConnection.onclose((error) => {
            console.log("SignalR connection closed", error);
            isConnected = false;
            isReconnecting = false;

            // Only auto-reconnect if not manually stopped
            if (!isManuallyStopped) {
                scheduleReconnection();
            }
            notifyPopupStatusChange();
        });

        signalRConnection.on("SendMessage", (sendChatMessageRequest) => {
            handleIncomingMessage(sendChatMessageRequest);
        });

        signalRConnection.on("GetListingInfo", (request) => {
            GetListingInfoRequest(request);
        });

        return true;
    } catch (error) {
        //console.error("Error initializing SignalR:", error);
        signalRConnection = null;
        return false;
    }
}

function scheduleReconnection() {
    if (isManuallyStopped) return; // Don't reconnect if we intentionally stopped

    clearTimeout(reconnectTimeout);
    console.log("Scheduling reconnection in .5 seconds...");
    reconnectTimeout = setTimeout(async () => {
        const hasFbTab = await hasAnyFacebookTab();
        if (hasFbTab && accountId) {
            startSignalRConnection();
        } else {
            console.log('No FB tab or no accountId, skipping reconnection.');
        }
    }, 500);
}

async function startSignalRConnection() {
    // Prevent concurrent connection attempts
    if (isReconnecting) {
        console.log("Reconnection already in progress");
        return;
    }

    const state = signalRConnection.state;

    if (state === signalR.HubConnectionState.Connected) {
        console.log("Already connected");
        return;
    }

    if (state === signalR.HubConnectionState.Connecting) {
        console.log("Connection already in progress");
        return;
    }

    try {
        isReconnecting = true;
        clearTimeout(reconnectTimeout);

        await signalRConnection.start();
        console.log("SignalR connected successfully");

        isConnected = true;
        isReconnecting = false;

        await registerExtensionUser();
        notifyPopupStatusChange();
        await retryFailedRequests();
    } catch (error) {
        //console.error("Failed to start SignalR connection:", error);
        isConnected = false;
        isReconnecting = false;

        // infinite reconnection
        scheduleReconnection();
    }
}

// Register the extension as a user
async function registerExtensionUser() {
    try {
        if (signalRConnection && isConnected && accountId) {

            var request = { accountId: accountId, userId: apiUserId };

            await signalRConnection.invoke("RegisterExtension", request);
            console.log("Extension registered with accountId:", accountId);
        } else {
            console.warn("Cannot register — not connected or no accountId.");
        }
    } catch (error) {
        console.error("Error registering extension user:", error);
    }
}


async function GetListingInfoRequest(request) {
    console.log("GetListingInfoRequest");

    const facebookTabs = await chrome.tabs.query({
        url: "*://*.facebook.com/*",
    });

    if (facebookTabs.length > 0) {
        try {
            await chrome.tabs.sendMessage(facebookTabs[0].id, {
                action: "getListingInfoRequest",
                data: request,
            });
            return;
        } catch (error) {
            console.log("Could not send default message to first tab:", facebookTabs[0].id);
        }
    }
}


// Handle incoming messages from app
async function handleIncomingMessage(sendChatMessageRequest) {

    console.log("Message received from app :", sendChatMessageRequest);

    sendChatMessageRequest.mediaBase64 = [];
    var mediaPaths = sendChatMessageRequest.mediaPaths;
    for (const path of mediaPaths) {
        const base64 = await getBase64FromUrl(path);
        sendChatMessageRequest.mediaBase64.push(base64);
    }
    console.log("processed media done.");

    const activeTabs = await chrome.tabs.query({
        url: "*://*.facebook.com/*",

    });

    if (activeTabs.length > 0) {
        try {
            await chrome.tabs.sendMessage(activeTabs[0].id, {
                action: "sendMessageToFacebook",
                data: sendChatMessageRequest,
            });
            return;
        } catch (error) {
            console.log("Could not send message coming from our app to facebook:", activeTabs[0].id);
        }
    }
}


//Send Message To Server
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    handleMessage(request, sender, sendResponse);
    return true; // keep port open for ALL async responses
});


async function handleMessage(request, sender, sendResponse) {
    if (request.key === 'logout') {
        authToken = null;
        apiUserId = null;
        accountId = null;
        await chrome.storage.local.remove(['authToken', 'accountId']);
        await disconnectSignalR();
        sendResponse({ success: true });
        return true;
    }

    if (request.key === 'getStatus') {
        sendResponse({
            isConnected,
            accountId,
            authToken: !!authToken, // just a boolean, don't expose the actual token
        });
        return true;
    }

    if (request.key === 'loginToApi') {
        const success = await loginToApi(request.username, request.password);
        sendResponse({ success });
        return true;
    }

    //all the authenticated api endpoint should be called below this. and not authenticated endpoints should be called above this..
    if (!authToken)
    {
        console.log("Extension not logged in.");
        return;
    }

    if (request.key === "syncListingDetail") {
        console.log("sending syncListingDetail: ", request.detail);

        const payload = {
            ...request.detail,
            accountId,  // ensure accountId always comes from background.js
        };

        try {
            const res = await apiFetch(`${remoteApiUrl}/api/sync/listing-info`, {
                method: "POST",
                body: JSON.stringify(payload),
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);
        }
        catch (err) {
            console.error('syncListingDetail failed, queuing for retry:', err);
            enqueueFailedRequest('syncListingDetail', payload);
        }

        return true;
    }

    if (request.key === "sendRawChunkToApi") {
        console.log("sending sendRawChunkToApi: ", request.detail);

        const payload = {
            ...request.detail,
            d: accountId,  // ensure accountId always comes from background.js
        };

        try {
            const res = await apiFetch(`${remoteApiUrl}/api/sync`, {
                method: "POST",
                body: JSON.stringify(payload),
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);
        }
        catch (err) {
            console.error('sendRawChunkToApi failed, queuing for retry:', err);
            enqueueFailedRequest('sendRawChunkToApi', payload);
        }

        return true;
    }

    if (request.key === "notifyAccountAuthState") {
        const { fbAccountId, isLoggedIn } = request.detail;
        var isInitialLogin = false;

        if (isLoggedIn && fbAccountId) {
            // Register account if not already registered
            if (!accountId) {
                const registered = await registerAccount(fbAccountId);
                if (!registered) {
                    console.error('Could not register account, skipping.');
                    enqueueFailedRequest('registerAccount', request.detail );
                    sendResponse({ success: false });
                    return true;
                }

                isInitialLogin = true;
            }


            if (isLoggedIn && isInitialLogin)
            {
                //naviagte to fb messages page so initial sync logic can run..

                const fbTabs = await chrome.tabs.query({ url: '*://*.facebook.com/*' });
                if (fbTabs.length > 0) {
                    // Only do initial sync on first tab
                    await chrome.tabs.update(fbTabs[0].id, {
                        url: 'https://www.facebook.com/messages/t/'
                    });
                }
            }

            var statusRequest = {
                ...request.detail,
                accountId,  // ensure accountId always comes from background.js
            };

            // Notify your API that this account is now online
            await apiFetch(`${remoteApiUrl}/api/account/${accountId}/status`, {
                method: 'PUT',
                body: JSON.stringify(statusRequest),
            });

            console.log('Account is logged in and registered:', accountId);

            // Task 3 will trigger SignalR here
            await connectSignalR();

        } else {
            // FB logged out — clear stored accountId
            console.log('FB logged out, clearing accountId.');
            accountId = null;
            await chrome.storage.local.remove('accountId');

            // Task 3 will disconnect SignalR here
            await disconnectSignalR();
        }

        sendResponse({ success: true });
        return true;
    }
}


//helper method
async function getBase64FromUrl(path) {
    path = `${remoteApiUrl}/${path}`;
    const response = await fetch(path);
    const blob = await response.blob();
    //await PrintLogs(path);
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}



chrome.runtime.onInstalled.addListener(async () => {
    await loadAuthToken();
    await loadAccountId();
    chrome.alarms.create('keepAlive', { periodInMinutes: 0.2 });

    // If extension was reinstalled while FB was open, reconnect
    if (accountId) {
        await connectSignalR();
    }
});



chrome.alarms.onAlarm.addListener(async (alarm) => {
    if (alarm.name === 'keepAlive') {
        console.log('Service worker kept alive.');

        // Only attempt reconnect if we have a FB tab and accountId
        // and connection dropped unintentionally
        if (!isManuallyStopped && accountId && !isConnected) {
            const hasFbTab = await hasAnyFacebookTab();
            if (hasFbTab) {
                console.log('Alarm: FB tab open but disconnected, reconnecting...');
                await connectSignalR();
            }
        }
    }
});



async function loadAuthToken() {
    const result = await chrome.storage.local.get(['authToken', 'apiUserId']);
    authToken = result.authToken || null;
    apiUserId = result.apiUserId || null;
}



async function loginToApi(usernameOrKey, password = null) {
    try {
        const body = password
            ? { email: usernameOrKey, password: password }   // username/password login
            : { apiKey: usernameOrKey };               // api key login

        const res = await fetch(`${remoteApiUrl}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        });

        const data = await res.json();

        if (data.isSuccess && data.data?.token) {
            authToken = data.data.token;
            apiUserId = data.data.userId;

            await chrome.storage.local.set({
                authToken: data.data.token,
                apiUserId: data.data.userId
            });
            console.log('Logged in, token saved. UserId:', data.data.userId);
            await recheckFbAuth(); // 
            return true;
        }
    } catch (err) {
        console.error('Login failed:', err);
    }
    return false;
}


// Helper: authenticated fetch wrapper
async function apiFetch(url, options = {}) {
    return fetch(url, {
        ...options,
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
            ...(authToken ? { Authorization: `Bearer ${authToken}` } : {}),
            ...(options.headers || {}),
        },
    });
}



async function loadAccountId() {
    const result = await chrome.storage.local.get('accountId');
    accountId = result.accountId || null;
    console.log('Loaded accountId from storage:', accountId);
}



async function registerAccount(fbAccountId) {
    try {
        const res = await apiFetch(`${remoteApiUrl}/api/account/register`, {
            method: 'POST',
            body: JSON.stringify({ fbAccountId }),
        });

        const response = await res.json();

        if (response.isSuccess && response.data && response.data.accountId) {
            accountId = response.data.accountId;
            await chrome.storage.local.set({ accountId });
            console.log('Account registered, accountId:', accountId);
            return true;
        }
    } catch (err) {
        console.error('Account registration failed:', err);
    }
    return false;
}


// Connects SignalR only if conditions are met
async function connectSignalR() {
    if (!accountId) {
        console.log('No accountId yet, cannot connect SignalR.');
        return;
    }

    if (!authToken) {
        console.log('Extension not logged in, cannot connect SignalR.');
        return;
    }

    const hasFbTab = await hasAnyFacebookTab();
    if (!hasFbTab) {
        console.log('No FB tab open, skipping SignalR connect.');
        return;
    }

    isManuallyStopped = false;

    const built = await initializeSignalR();
    if (built) {
        await startSignalRConnection();
    }
}



// Disconnects SignalR intentionally
async function disconnectSignalR() {
    if (!signalRConnection) return;

    const state = signalRConnection.state;
    if (state === signalR.HubConnectionState.Disconnected) return;

    console.log('Disconnecting SignalR intentionally...');
    isManuallyStopped = true;
    clearTimeout(reconnectTimeout);

    try {
        await signalRConnection.stop();
        notifyPopupStatusChange();
        signalRConnection = null; // Reset so it can be rebuilt fresh next time
        isConnected = false;
        console.log('SignalR disconnected.');

    } catch (err) {
        console.error('Error disconnecting SignalR:', err);
    }
}



// Check if any Facebook tab is currently open
async function hasAnyFacebookTab() {
    const tabs = await chrome.tabs.query({ url: '*://*.facebook.com/*' });
    return tabs.length > 0;
}




// Disconnect SignalR when all FB tabs are closed
chrome.tabs.onRemoved.addListener(async (tabId) => {
    const hasFbTab = await hasAnyFacebookTab();
    if (!hasFbTab) {
        console.log('All FB tabs closed, disconnecting SignalR.');
        await disconnectSignalR();
    }
});



// Disconnect when user navigates away from FB on last remaining FB tab
chrome.tabs.onUpdated.addListener(async (tabId, changeInfo) => {
    if (!changeInfo.url) return; // Only care about URL changes

    const isFbUrl = changeInfo.url.includes('facebook.com');
    if (!isFbUrl) {
        // A tab navigated away — check if any FB tab still open
        const hasFbTab = await hasAnyFacebookTab();
        if (!hasFbTab) {
            console.log('No more FB tabs, disconnecting SignalR.');
            await disconnectSignalR();
        }
    } else {
        // A tab navigated TO facebook — try to connect
        await connectSignalR();
    }
});



function notifyPopupStatusChange() {
    updateBadge(isConnected);
    chrome.runtime.sendMessage({
        key: 'statusChanged',
        isConnected,
        accountId,
    }).catch(() => {
        // Popup not open — ignore
    });
}


function updateBadge(isConnected) {
    const color = isConnected ? '#42c96b' : '#ff4d4d';
    chrome.action.setBadgeText({ text: ' ' });
    chrome.action.setBadgeBackgroundColor({ color });
}


async function recheckFbAuth() {
    const fbTabs = await chrome.tabs.query({ url: '*://*.facebook.com/*' });
    for (const tab of fbTabs) {
        chrome.tabs.sendMessage(tab.id, { action: 'recheckAuth' }).catch(() => { });
    }
}
