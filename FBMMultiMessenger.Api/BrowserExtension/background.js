// Import SignalR library

importScripts("./signalr.min.js");

let signalRConnection = null;
let isConnected = false;
let reconnectTimeout = null;
let isReconnecting = false;

async function initializeSignalR() {
    try {
        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${localServerUrl}/extensionHub`, {
                withCredentials: true,
            })
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        signalRConnection.onreconnecting(() => {
            console.log("SignalR reconnecting...");
            isConnected = false;
            isReconnecting = true;
        });

        signalRConnection.onreconnected(() => {
            console.log("SignalR reconnected");
            isConnected = true;
            isReconnecting = false;

            registerExtensionUser().catch((err) => {
                console.error("Failed to register user after reconnect:", err);
            });
        });

        signalRConnection.onclose((error) => {
            console.log("SignalR connection closed", error);
            isConnected = false;
            isReconnecting = false;

            // Always try to reconnect, no limits

            scheduleReconnection();
        });

        signalRConnection.on("SendMessage", (sendChatMessageRequest) => {
            handleIncomingMessage(sendChatMessageRequest);
        });
        signalRConnection.on("HandleDefaultMessage", (defaultMessage) => {
            handleDefaultMessage(defaultMessage);
        });

        await startSignalRConnection();
        return true;
    } catch (error) {
        console.error("Error initializing SignalR:", error);
        return false;
    }
}

function scheduleReconnection() {
    clearTimeout(reconnectTimeout);

    console.log("Scheduling reconnection in 5 seconds...");

    reconnectTimeout = setTimeout(() => {
        startSignalRConnection();
    }, 5000);
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
    } catch (error) {
        console.error("Failed to start SignalR connection:", error);
        isConnected = false;
        isReconnecting = false;

        // infinite reconnection
        scheduleReconnection();
    }
}

// Register the extension as a user
async function registerExtensionUser() {
    try {
        if (signalRConnection && isConnected) {
            await signalRConnection.invoke(
                "RegisterExtension",
                `Extension_${fbAccountId}`
            );
            console.log("Extension registered as user");
        }
    } catch (error) {
        console.error("Error registering extension user:", error);
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

    // Try active tab first
    const activeTabs = await chrome.tabs.query({
        url: "*://*.facebook.com/messages*",
        active: true,
        currentWindow: true,
    });

    if (activeTabs.length > 0) {
        try {
            await chrome.tabs.sendMessage(activeTabs[0].id, {
                action: "sendMessageToFacebook",
                data: sendChatMessageRequest,
            });
            return;
        } catch (error) {
            console.log("Could not send to active tab:", activeTabs[0].id);
        }
    }

    // Try any Facebook messages tab
    const allTabs = await chrome.tabs.query({
        url: "*://*.facebook.com/messages*",
    });

    if (allTabs.length > 0) {
        try {
            await chrome.tabs.sendMessage(allTabs[0].id, {
                action: "sendMessageToFacebook",
                data: sendChatMessageRequest,
            });
            return;
        } catch (error) {
            console.log("Could not send to first tab:", allTabs[0].id);
        }
    }

    // No tabs found - create new one
    console.log("No Facebook messages tabs found, creating new tab...");
    const newTab = await chrome.tabs.create({
        url: "https://www.facebook.com/messages",
        active: true,
    });

    // Wait for tab to load before sending message
    chrome.tabs.onUpdated.addListener(function listener(tabId, changeInfo) {
        if (tabId === newTab.id && changeInfo.status === "complete") {
            chrome.tabs.onUpdated.removeListener(listener);

            setTimeout(() => {
                chrome.tabs
                    .sendMessage(newTab.id, {
                        action: "sendMessageToFacebook",
                        data: sendChatMessageRequest,
                    })
                    .catch((error) => {
                        console.log("Could not send to new tab:", error);
                    });
            }, 2000); // Wait 2 seconds for page to fully load
        }
    });
}

//Handle default message requests from app
async function handleDefaultMessage(defaultMessage) {
    console.log("Default message request :", defaultMessage);

    // Try active tab first
    const activeTabs = await chrome.tabs.query({
        url: "*://*.facebook.com/messages*",
        active: true,
        currentWindow: true,
    });

    if (activeTabs.length > 0) {
        try {
            await chrome.tabs.sendMessage(activeTabs[0].id, {
                action: "setDefaultMessage",
                data: defaultMessage,
            });
            return;
        } catch (error) {
            console.log("Could not send to active tab:", activeTabs[0].id);
        }
    }

    // Try any Facebook messages tab
    const allTabs = await chrome.tabs.query({
        url: "*://*.facebook.com/messages*",
    });

    if (allTabs.length > 0) {
        try {
            await chrome.tabs.sendMessage(allTabs[0].id, {
                action: "setDefaultMessage",
                data: defaultMessage,
            });
            return;
        } catch (error) {
            console.log("Could not send to first tab:", allTabs[0].id);
        }
    }

    // No tabs found - create new one
    console.log("No Facebook messages tabs found, creating new tab...");
    const newTab = await chrome.tabs.create({
        url: "https://www.facebook.com/messages",
        active: true,
    });

    // Wait for tab to load before sending message
    chrome.tabs.onUpdated.addListener(function listener(tabId, changeInfo) {
        if (tabId === newTab.id && changeInfo.status === "complete") {
            chrome.tabs.onUpdated.removeListener(listener);

            setTimeout(() => {
                chrome.tabs
                    .sendMessage(newTab.id, {
                        action: "setDefaultMessage",
                        data: defaultMessage,
                    })
                    .catch((error) => {
                        console.log("Could not send to new tab:", error);
                    });
            }, 2000); // Wait 2 seconds for page to fully load
        }
    });
}

//Send Message To Server
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {

    if (request.key === "sendMessageToApi") {
        console.log("message sent to API: ", request.detail);
        fetch(`${localServerUrl}/api/chat/message`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                Accept: "application/json",
            },
            body: JSON.stringify(request.detail),
        })
            .then((res) => res.json())
            .then((data) => {
                sendResponse(data); // Send back to content.js
            })
            .catch((err) => {
                console.error("API error:", err);
                sendResponse({ error: err.message });
            });
        return true;
    }


    if (request.key === "notifyAccountAuthState") {
        let detail = request.detail;

        fetch(`${localServerUrl}/api/accounts/${detail.accountId}/status`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                Accept: "application/json",
            },
            body: JSON.stringify(detail),
        })
            .then((res) => res.json())
            .then((data) => {
                sendResponse(data); // Send back to content.js
            })
            .catch((err) => {
                console.error("API error:", err);
                sendResponse({ error: err.message });
            });
        return true;
    }
});

//helper method
async function getBase64FromUrl(path) {
    path = `${remoteApiUrl}/${path}`;
    const response = await fetch(path);
    const blob = await response.blob();

    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}

chrome.runtime.onInstalled.addListener(() => {
    initializeSignalR();


});
