// Import SignalR library

importScripts("./signalr.min.js");

// ==================== Debug log capture ====================
// Anti-detect browsers (e.g. ixBrowser) disable the page console, and a service worker's logs/network are
// not visible in the page DevTools anyway. So we mirror every background console.* call into a persistent
// store (chrome.storage.local, survives service-worker restarts). To read them:
//   • run  showLogs()      in the service-worker console, OR
//   • run  fbmShowLogs()   in an open Facebook page's console
// either shows the captured logs as a popup ON the Facebook page (works even when the console is muted).
const __fbmLogBuffer = [];
let __fbmLogWriteChain = Promise.resolve();

function __fbmFmt(a) {
    if (a instanceof Error) return (a.message || String(a)) + (a.stack ? ('\n' + a.stack) : '');
    if (typeof a === 'string') return a;
    try { return JSON.stringify(a); } catch (e) { return String(a); }
}

function __fbmStoreLog(level, args) {
    let text;
    try { text = Array.prototype.map.call(args, __fbmFmt).join(' '); }
    catch (e) { text = String(args); }

    const line = new Date().toISOString() + ' [' + level + '] ' + text;

    __fbmLogBuffer.push(line);
    if (__fbmLogBuffer.length > 1000) __fbmLogBuffer.shift();

    // Persist (serialized to avoid read-modify-write races) so logs survive SW restarts.
    __fbmLogWriteChain = __fbmLogWriteChain.then(async () => {
        try {
            const stored = await chrome.storage.local.get('fbmLogs');
            const fbmLogs = stored.fbmLogs || [];
            fbmLogs.push(line);
            if (fbmLogs.length > 1000) fbmLogs.splice(0, fbmLogs.length - 1000);
            await chrome.storage.local.set({ fbmLogs });
        } catch (e) { }
    });
}

// Wrap console so ALL existing background logs are captured without editing every call site.
(function () {
    const orig = { log: console.log, warn: console.warn, error: console.error };
    console.log = function () { __fbmStoreLog('LOG', arguments); try { orig.log.apply(console, arguments); } catch (e) { } };
    console.warn = function () { __fbmStoreLog('WARN', arguments); try { orig.warn.apply(console, arguments); } catch (e) { } };
    console.error = function () { __fbmStoreLog('ERROR', arguments); try { orig.error.apply(console, arguments); } catch (e) { } };
})();

// Explicit helper (same sink as console.log): prints AND stores.
function bgLog() { console.log.apply(console, arguments); }

// Reads captured logs and shows them as a popup on an open Facebook tab. Call in the SW console: showLogs()
self.showLogs = async function () {
    let logs = [];
    try {
        const stored = await chrome.storage.local.get('fbmLogs');
        logs = stored.fbmLogs || [];
    } catch (e) { }
    if (!logs.length) logs = __fbmLogBuffer.slice();

    try {
        const tabs = await chrome.tabs.query({ url: '*://*.facebook.com/*' });
        const target = tabs.find(t => t.active) || tabs[0];
        if (!target) {
            console.log('showLogs: open a Facebook tab first so the logs can be shown there.');
            return logs;
        }
        await chrome.tabs.sendMessage(target.id, { action: 'showFbmLogs', logs });
    } catch (e) {
        console.log('showLogs: could not reach the Facebook content script:', e && e.message);
    }
    return logs;
};

// Clears the captured logs. Call in the SW console: clearLogs()
self.clearLogs = async function () {
    __fbmLogBuffer.length = 0;
    try { await chrome.storage.local.remove('fbmLogs'); } catch (e) { }
    console.log('FBM logs cleared.');
};
// ==================== /Debug log capture ====================

console.log('Scrapping Background script running.');

//var remoteApiUrl = "https://api.fbmmessenger.com";
var remoteApiUrl = "https://localhost:7095";
var remoteAPISignalRUrl = `${remoteApiUrl}/chathub`;
var accountId = null;
var apiUserId = null;

// Add near the top with other variables
var authToken = null;
var apiKey = null;
var lastRegistrationError = null;
var __FBM_AUTO_OPEN_MESSENGER__ = "%%FBM_AUTO_OPEN_MESSENGER%%";
var __FBM_ROBO_API_KEY__ = "%%FBM_ROBO_API_KEY%%";

function isAuthenticated() {
    return !!(apiKey || authToken);
}

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
                const result = await registerAccount(request.payload.fbAccountId);
                if (!result.ok) {
                    if (!result.retryable) {
                        console.error('Registration failed (not retrying):', result.message);
                        continue;
                    }
                    throw new Error(result.message || 'Registration still failing');
                }

                await apiFetch(`${remoteApiUrl}/api/account/${accountId}/status`, {
                    method: 'POST',
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
        if (apiKey && !apiUserId) {
            await resolveApiUserId();
        }

        if (!apiUserId) {
            console.warn('Cannot register — apiUserId is missing.');
            return;
        }

        if (signalRConnection && isConnected && accountId) {

            var request = { accountId: accountId, userId: apiUserId };

            await signalRConnection.invoke("RegisterExtension", request);
            console.log("Extension registered with accountId:", accountId, "userId:", apiUserId);
        } else {
            console.warn(`Cannot register — not connected or no accountId. isConnected: ${isConnected}, accountId: ${accountId}`);
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
    if (request.key === 'getFbmLogs') {
        let logs = [];
        try {
            const stored = await chrome.storage.local.get('fbmLogs');
            logs = stored.fbmLogs || [];
        } catch (e) { }
        if (!logs.length) logs = __fbmLogBuffer.slice();
        sendResponse({ logs });
        return true;
    }

    if (request.key === 'logout') {
        authToken = null;
        accountId = null;
        // Keep apiKey + apiUserId when Robo-injected key remains
        const keysToRemove = ['authToken', 'accountId'];
        if (!apiKey) {
            apiUserId = null;
            keysToRemove.push('apiUserId');
        }
        await chrome.storage.local.remove(keysToRemove);
        await disconnectSignalR();
        sendResponse({ success: true });
        return true;
    }

    if (request.key === 'getStatus') {
        sendResponse({
            isConnected,
            accountId,
            authToken: isAuthenticated(), // boolean: API key or JWT present
            hasApiKey: !!apiKey,
            apiKey: apiKey || null,
            registrationError: lastRegistrationError,
        });
        return true;
    }

    if (request.key === 'loginToApi') {
        const success = await loginToApi(request.username, request.password);
        sendResponse({ success });
        return true;
    }

    //all the authenticated api endpoint should be called below this. and not authenticated endpoints should be called above this..
    if (!isAuthenticated())
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

        console.log('running notifyAccountAuthState');

        const { fbAccountId, isLoggedIn } = request.detail;
        var isInitialLogin = false;

        if (isLoggedIn && fbAccountId) {
            // Register account if not already registered
            if (!accountId) {
                const result = await registerAccount(fbAccountId);
                if (!result.ok) {
                    console.error('Could not register account:', result.message);
                    lastRegistrationError = result.message;
                    notifyPopupRegistrationError();

                    // Only retry when the server was unreachable — not for validation errors
                    if (result.retryable) {
                        enqueueFailedRequest('registerAccount', request.detail);
                    }

                    sendResponse({ success: false, message: result.message });
                    return true;
                }

                isInitialLogin = true;
            }


            // Only Robo-packed builds inject __FBM_AUTO_OPEN_MESSENGER__ = true.
            // Standalone extension installs leave this unset and must not auto-navigate.
            if (isLoggedIn && isInitialLogin
                && typeof __FBM_AUTO_OPEN_MESSENGER__ !== 'undefined'
                && __FBM_AUTO_OPEN_MESSENGER__)
            {
                // Navigate to FB messages so initial sync logic can run (Robo browser launch only).
                const fbTabs = await chrome.tabs.query({ url: '*://*.facebook.com/*' });
                if (fbTabs.length > 0) {
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
                method: 'POST',
                body: JSON.stringify(statusRequest),
            });

            console.log('Account is logged in and registered:', accountId);

            // Task 3 will trigger SignalR here
            await connectSignalR();

        } else {
            // FB logged out — clear stored accountId
            console.log(`FB logged out, clearing accountId. ${isLoggedIn}: isLoggedIn, fbAccountId ${fbAccountId}`);
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
    await OnInitialized();
});

// Service worker restarts — restore auth from storage / Robo inject
(async () => {
    console.log("Service worker restarts — restore auth from storage / Robo inject");

    await OnInitialized();
})();

async function OnInitialized() {
    await loadAuth();
    await loadAccountId();
    chrome.alarms.create('keepAlive', { periodInMinutes: 0.2 });

    if (!accountId) {
        recheckFbAuth();
        await delay(2000); //delay so if inject.js has time to callback.
    }

    // If extension was reinstalled while FB was open, reconnect
    if (accountId) {
        await connectSignalR();
    }
}



chrome.alarms.onAlarm.addListener(async (alarm) => {
    if (alarm.name === 'keepAlive') {

        // Only attempt reconnect if we have a FB tab and accountId
        // and connection dropped unintentionally

        if (!accountId) {
            recheckFbAuth(); //!accountId means fb not logged in, or yet we do not know recheckFbAuth will call inject.js to recheck
            //that will give us a callback which will eventually run notifyAccountAuthState if fb is logged in notifyAccountAuthState
            //will call connectSignalR, so we are returning.
            return;
        }

        if (accountId && !isManuallyStopped && !isConnected) {
            const hasFbTab = await hasAnyFacebookTab();

            if (!hasFbTab)
            {
                console.log('No Fb Tab');
                return;
            }

            if (hasFbTab) {
                console.log('Alarm: FB tab open but disconnected, reconnecting...');
                await connectSignalR();
            }
        }
    }
});



async function loadAuth() {
    // Robo injects __FBM_ROBO_API_KEY__ at the top of this file when packing the extension
    if (typeof __FBM_ROBO_API_KEY__ !== 'undefined' && __FBM_ROBO_API_KEY__) {
        apiKey = __FBM_ROBO_API_KEY__;
        await chrome.storage.local.set({ apiKey });
    }

    const result = await chrome.storage.local.get(['authToken', 'apiUserId', 'apiKey']);
    authToken = result.authToken || null;
    apiUserId = result.apiUserId || null;
    if (!apiKey) {
        apiKey = result.apiKey || null;
    }

    if (apiKey && !apiUserId) {
        await resolveApiUserId();
    }
}



async function resolveApiUserId() {
    if (!apiKey) {
        return false;
    }

    try {
        const res = await apiFetch(`${remoteApiUrl}/api/account/extension-identity`, {
            method: 'GET',
        });
        const data = await res.json();

        if (data.isSuccess && data.data?.userId) {
            apiUserId = data.data.userId;
            await chrome.storage.local.set({ apiUserId });
            console.log('Resolved apiUserId from API key:', apiUserId);
            return true;
        }

        console.error('Failed to resolve apiUserId:', data.message || res.status);
    } catch (err) {
        console.error('resolveApiUserId failed:', err);
    }

    return false;
}



async function loginToApi(usernameOrKey, password = null) {
    // Prefer storing a Multi Messenger API key directly (no JWT login)
    if (!password && usernameOrKey) {
        apiKey = usernameOrKey;
        await chrome.storage.local.set({ apiKey });
        console.log('API key saved.');
        await resolveApiUserId();
        await recheckFbAuth();
        return !!apiUserId;
    }

    try {
        const body = { email: usernameOrKey, password: password };

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
            await recheckFbAuth();
            return true;
        }
    } catch (err) {
        console.error('Login failed:', err);
    }
    return false;
}


// Helper: authenticated fetch wrapper
async function apiFetch(url, options = {}) {
    const authHeaders = {};
    if (apiKey) {
        authHeaders['X-API-KEY'] = apiKey;
    } else if (authToken) {
        authHeaders['Authorization'] = `Bearer ${authToken}`;
    }

    return fetch(url, {
        ...options,
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
            ...authHeaders,
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
        console.log('Registerering account.');

        const res = await apiFetch(`${remoteApiUrl}/api/account/register`, {
            method: 'POST',
            body: JSON.stringify({ fbAccountId }),
        });

        const response = await res.json();

        if (response.isSuccess && response.data && response.data.accountId) {
            accountId = response.data.accountId;
            lastRegistrationError = null;
            await chrome.storage.local.set({ accountId });
            console.log('Account registered, accountId:', accountId);
            return { ok: true };
        }

        console.log('Account registeration failed with message:', response.message);

        return {
            ok: false,
            message: response.message || 'Account registration failed.',
            retryable: false,
        };
    } catch (err) {
        console.error('Account registration failed:', err);
        return {
            ok: false,
            message: 'Could not reach Multi Messenger server.',
            retryable: true,
        };
    }
}

function notifyPopupRegistrationError() {
    chrome.runtime.sendMessage({
        key: 'registrationError',
        message: lastRegistrationError,
    }).catch(() => {
        // Popup not open — ignore
    });
}


// Connects SignalR only if conditions are met
async function connectSignalR() {
    if (!accountId) {
        console.log('No accountId yet, cannot connect SignalR.');
        return;
    }

    if (!isAuthenticated()) {
        console.log('Extension not logged in, cannot connect SignalR.');
        return;
    }

    if (apiKey && !apiUserId) {
        await resolveApiUserId();
    }

    if (!apiUserId) {
        console.log('No apiUserId yet, cannot connect SignalR.');
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
    console.log('running recheckFbAuth in bj');

    const fbTabs = await chrome.tabs.query({ url: '*://*.facebook.com/*' });
    for (const tab of fbTabs) {
        chrome.tabs.sendMessage(tab.id, { action: 'recheckAuth' }).catch(() => { });
    }
}

function delay(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
}