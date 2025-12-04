var root = document.documentElement;
root.addEventListener("sendMessageToApi", function (e) {
    chrome.runtime.sendMessage({
        key: "sendMessageToApi",
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

var defaultTemplate = `{
    "root": {
        "children": [{
            "children": [],
            "direction": "ltr",
            "format": "",
            "indent": 0,
            "type": "paragraph",
            "version": 1
        }],
        "direction": "ltr",
        "format": "",
        "indent": 0,
        "type": "root",
        "version": 1
    }
}`;

function setMessage(data) {
    window.postMessage(
        {
            type: "SET_FACEBOOK_MESSAGE",
            data: data,
            defaultTemplate: defaultTemplate,
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