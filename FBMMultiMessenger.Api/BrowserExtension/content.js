var root = document.documentElement;


root.addEventListener("notifyAccountAuthState", function (e) {
    chrome.runtime.sendMessage({
        key: "notifyAccountAuthState",
        detail: e.detail,
    });
});

root.addEventListener("sendRawChunkToApi", function (e) {
    chrome.runtime.sendMessage({
        key: "sendRawChunkToApi",
        detail: e.detail,
    });
});

root.addEventListener("syncListingDetail", function (e) {
    debugger;

    chrome.runtime.sendMessage({
        key: "syncListingDetail",
        detail: e.detail,
    });
});

// Listen for messages from background script (SignalR messages)
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {

    if (request.action === 'recheckAuth') {
        window.postMessage({ type: 'RECHECK_AUTH' }, '*');
        sendResponse({ success: true });
    }

    if (request.action === 'getListingInfoRequest') {
        window.postMessage({
            type: 'getListingInfoRequest',
            data: request.data
        }, '*');
        sendResponse({ success: true });
    }

    if (request.action === "sendMessageToFacebook") {
        try {
            let data = request.data;
            let fbChatId = data.fbChatId;

            if (fbChatId) {
                setMessage(data);
            }
        } catch (error) {
            console.error("Error sending message to Facebook:", error);
        }
        sendResponse({ success: true });
    }
});


function setMessage(data) {
    window.postMessage(
        {
            type: "SET_FACEBOOK_MESSAGE",
            data: data,
        },

        "*"
    );
}
