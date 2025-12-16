var root = document.documentElement;


root.addEventListener("sendMessageToApi", function (e) {
    chrome.runtime.sendMessage({
        key: "sendMessageToApi",
        detail: e.detail,
    });
});

root.addEventListener("notifyAccountAuthState", function (e) {
    chrome.runtime.sendMessage({
        key: "notifyAccountAuthState",
        detail: e.detail,
    });
});

// Listen for messages from background script (SignalR messages)
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === "sendMessageToFacebook") {
        try {
            let data = request.data;
            let fbChatId = data.fbChatId;

            if (fbChatId) {
                //PersistMessageFromApp();

                setMessage(data);
            }
        } catch (error) {
            console.error("Error sending message to Facebook:", error);
        }
        sendResponse({ success: true });
    }
});


chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === "setDefaultMessage") {
        try {
            let defaultMessage = request.data;
            setDefaultMessage(defaultMessage);

        } catch (error) {
            console.error("Error setting default message", error);
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



function setDefaultMessage(defaultMessage) {
    window.postMessage(
        {
            type: "SET_DEFAULT_MESSAGE",
            data: defaultMessage,
        },

        "*"
    );
}


function PersistMessageFromApp() {
    window.postMessage(
        {
            type: "Persist_Message_From_App",
        },
        "*"
    );
}