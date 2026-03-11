const accountConnectionStatus = {
    Online: 1,
    Offline: 2,
    Starting: 3,
}

const accountAuthStatus = {
    Idle: 1,
    LoggedIn: 2,
    LoggedOut: 3,
}
const accountReason = {
    ExpiredOrInvalidCookie: 1,
    AssignedToLocalServer: 3,
}

// Global FIFO queue - array of objects
let pendingMessages = [];

let messageQueue = [];
let isProcessingMessage = false;

let globalDefaultTemplate = `{
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

(function () {
    // Save the original WebSocket constructor
    const originalWebSocket = window.WebSocket;

    // Override the WebSocket constructor
    window.WebSocket = function (...args) {
        const wsInstance = new originalWebSocket(...args);

        // Intercept messages sent through the WebSocket
        const originalSend = wsInstance.send;
        wsInstance.send = function (data) {
            try {
                let text = null;

                // Check for Uint8Array first, then ArrayBuffer
                if (data instanceof Uint8Array) {
                    text = new TextDecoder().decode(data);
                } else if (data instanceof ArrayBuffer) {
                    text = new TextDecoder().decode(new Uint8Array(data));
                }

                if (text && text.includes("otid")) {
                    HandlerSentMessage(text);
                }
            } catch (err) {
                console.error("Error processing sent message:", err);
            }

            return originalSend.apply(this, arguments);
        };

        // Listen for messages received from the WebSocket
        wsInstance.addEventListener("message", function (event) {
            let receivedData = event.data;

            if (receivedData instanceof ArrayBuffer) {
                // Handle ArrayBuffer messages

                try {
                    const bytes = new Uint8Array(receivedData);
                    const text = new TextDecoder().decode(bytes); // only for the includes() checks

                    const hasInsertMessage = text.includes("insertMessage");
                    const hasSyncMessages = text.includes("insertNewMessageRange");

                    if (hasInsertMessage || hasSyncMessages) {
                        // Convert raw bytes to base64 — never decoded on client side
                        const base64Chunk = btoa(String.fromCharCode(...bytes));

                        var root = document.documentElement;
                        root.dispatchEvent(new CustomEvent("sendRawChunkToApi", {
                            detail: {
                                fbAccountId: extractUserId(),
                                chunk: base64Chunk,
                                pendingMessages: [...pendingMessages]
                            }
                        }));
                    }
                }
                catch (ex) {

                }
            }
        });

        return wsInstance; // Return the modified WebSocket instance
    };

    function extractJsonPayloadForSentMessages(rawMessage) {
        const jsonStart = rawMessage.indexOf('{"app_id"');
        if (jsonStart !== -1) {
            return rawMessage.substring(jsonStart);
        } else {
            throw new Error("No JSON found in the message");
        }
    }

    function HandlerSentMessage(messageData) {
        try {
            messageData = extractJsonPayloadForSentMessages(messageData);

            // Parse the JSON data
            const data = JSON.parse(messageData);
            data.payload = JSON.parse(data.payload);

            if (Array.isArray(data.payload.tasks)) {
                data.payload.tasks = data.payload.tasks.map((task) => {
                    try {
                        task.payload = JSON.parse(task.payload);
                        return JSON.parse(task);
                    } catch {
                        return task; // leave as-is if not valid JSON
                    }
                });
            }

            var tasks = data.payload.tasks;
            console.log(data);
            for (const task of data.payload.tasks) {
                if (task?.payload && task.payload.hasOwnProperty("otid")) {
                    //fifo
                    const otidExists = pendingMessages.some(
                        (msg) => msg.otid === task.payload.otid
                    );

                    var fbMessageReplyId = task?.payload?.reply_metadata?.reply_source_id ?? null;

                    if (!otidExists) {
                        const pending = pendingMessages.find((msg) => !msg.otid);
                        if (pending) {
                            pending.otid = task.payload.otid;
                            pending.fbMessageReplyId = fbMessageReplyId;
                        }
                    }
                }
            }
        } catch (error) { }
    }

    // Restore original properties and methods
    window.WebSocket.prototype = originalWebSocket.prototype;
})();

//It will listen for messages from content.js
window.addEventListener("message", (event) => {
    // Make sure it’s from our own extension, not other scripts
    if (event.source !== window) return;

    if (event.data.type === "Print_Logs") {
        PringLogs(event.data.data);
    }

    if (event.data.type === "SET_FACEBOOK_MESSAGE") {
        messageQueue.push({
            messageData: event.data.data,
        });

        // Start processing queue
        processMessageQueue();
    }

    if (event.data.type === 'RECHECK_AUTH') {
        console.log('Rechecking FB auth on demand...');
        var isLoggedIn = isAccountLoggedIn(getCookie('c_user'), getEmailInput());
        NotifyAccountAuthStatus(isLoggedIn);
    }

    if (event.data.type === 'getListingInfoRequest') {
        console.log('getListingInfoRequest on demand...');
        var data = event.data.data;
        SyncListingInfo(data.fbChatId, data.chatId);
    }
});

async function processMessage(messageData, fbChatId) {
    return new Promise(async (resolve) => {
        let maxAttempts = 10;
        let attempts = 0;
        let input;
        let textMessageInputInterval;

        await NavigateToRequestedChat(messageData.fbChatId);

        textMessageInputInterval = setInterval(async () => {
            attempts++;
            input = document.querySelector(".notranslate");

            if (input) {
                //console.log("Message interval id:", textMessageInputInterval);
                clearInterval(textMessageInputInterval);
                //console.log("Input found:", input);

                var textMessage = messageData.message;
                let offlineUniqueId = messageData.offlineUniqueId;

                try {
                    pendingMessages.push({
                        uniqueId: offlineUniqueId,
                        timestamp: Date.now(),
                    });
                } catch (err) { }

                //clearing any text that is set before..
                HandleTextMessage(input, '');

                if (textMessage) {
                    HandleTextMessage(input, textMessage);
                }

                const mediaBase64s = messageData.mediaBase64;
                mediaBase64s.forEach((mediaBase64) => {
                    const blob = Base64T0Blob(mediaBase64);
                    const isVideo = mediaBase64.startsWith('data:video');

                    let mediaFile;
                    if (isVideo) {
                        // Create video file with appropriate extension and MIME type
                        mediaFile = new File([blob], "video.mp4", {
                            type: "video/mp4", // or "video/webm", "video/quicktime" for MOV
                        });
                    } else {
                        // Create image file
                        mediaFile = new File([blob], "image.jpg", {
                            type: "image/jpeg",
                        });
                    }

                    uploadMedia(mediaFile);
                });

                setTimeout(() => {
                    TriggerEnterEvent(input);
                    resolve(); // Message sent, resolve promise
                }, 300);
            } else if (attempts >= maxAttempts) {
                console.log(`Failed to find input field after ${attempts} attempts`);
                clearInterval(textMessageInputInterval);
                resolve(); // Failed but resolve to continue queue
            } else {
                console.log(
                    `Attempting to find input field... attempt ${attempts} of ${maxAttempts}`
                );
            }

            await delay(500);
        }, 0);
    });
}

// Process queue one by one
async function processMessageQueue() {
    if (isProcessingMessage || messageQueue.length === 0) return;

    isProcessingMessage = true;

    while (messageQueue.length > 0) {
        const { messageData } = messageQueue.shift();
        try {
            await processMessage(messageData);
        }
        catch (err) {
            console.error("Error while sending message inside while loop processMessageQueue:", err);
        }
    }

    isProcessingMessage = false;
}

function HandleTextMessage(input, message) {
    if (!input || !input.__lexicalEditor) {
        console.log("Input not found where the text should be inserted.");
        return false;
    }

    var jsonMessageTemplate = JSON.parse(globalDefaultTemplate);

    var lines = message.split("\n");
    for (var i = 0; i < lines.length; i++) {
        var line = lines[i];
        jsonMessageTemplate.root.children[0].children.push(AddText(line));
        if (i != lines.length - 1) {
            jsonMessageTemplate.root.children[0].children.push(AddLineBreak());
        }
    }

    var newState = input.__lexicalEditor.parseEditorState(
        JSON.stringify(jsonMessageTemplate)
    );
    input.__lexicalEditor.setEditorState(newState);

    return true;
}

function AddText(text) {
    var newTextNode = {
        detail: 0,
        format: 0,
        mode: "normal",
        style: "",
        text: text,
        type: "text",
        version: 1,
    };

    return newTextNode;
}

function AddLineBreak() {
    var lineBreak = {
        type: "linebreak",
        version: 1,
    };

    return lineBreak;
}

function Base64T0Blob(base64) {
    var byteCharacters = atob(base64.split(",")[1]);
    var byteNumbers = new Array(byteCharacters.length);
    for (var i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    var byteArray = new Uint8Array(byteNumbers);
    var blob = new Blob([byteArray], { type: "image/png" });

    return blob;
}

function uploadImage(imageFile) {
    // Locate the file input on the chat page
    var fileInput = document.querySelector('input[type="file"]');

    // Ensure the file input is present
    if (fileInput) {
        // Attach the image file to the file input
        Object.defineProperty(fileInput, "files", {
            value: [imageFile],
            writable: true,
        });

        // Create a file input change event
        // Dispatch the change event to trigger the file upload
        const changeEvent = new Event("change", { bubbles: true });
        fileInput.dispatchEvent(changeEvent);
    } else {
        console.error(
            "Input field not found where images/videos will be insereted."
        );
    }
}

function uploadMedia(mediaFile) {
    // Locate the file input on the chat page
    var fileInput = document.querySelector('input[type="file"]');

    // Ensure the file input is present
    if (fileInput) {
        // Check if the file input accepts this file type
        const acceptedTypes = fileInput.accept;
        console.log('Accepted file types:', acceptedTypes);

        // Create a DataTransfer object (more reliable than Object.defineProperty)
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(mediaFile);

        // Assign the files to the input
        fileInput.files = dataTransfer.files;

        // Dispatch change event
        const changeEvent = new Event("change", { bubbles: true });
        fileInput.dispatchEvent(changeEvent);

        // Also try dispatching input event (some sites listen to this instead)
        const inputEvent = new Event("input", { bubbles: true });
        fileInput.dispatchEvent(inputEvent);
    } else {
        console.error("Input field not found where media will be inserted.");
    }
}

function TriggerEnterEvent(input) {
    if (input) {
        const enterEvent = new KeyboardEvent("keydown", {
            key: "Enter",
            code: "Enter",
            keyCode: 13,
            bubbles: true,
            cancelable: true,
        });
        input.dispatchEvent(enterEvent);
    }
}

function TriggerClickEvent(input) {
    if (input) {
        const clickEvent = new MouseEvent("click", {
            bubbles: true,
            cancelable: true,
            view: window,
        });

        input.dispatchEvent(clickEvent);
    }
}

async function NavigateToRequestedChat(fbChatId) {
    const expectedUrl = `messages/t/${fbChatId}`;
    const currentUrl = window.location.href;
    var currentChatId = currentUrl.split('/').pop();

    const marketPlaceElement = document.querySelector(
        'div[aria-label="Chats"][role="grid"] [data-virtualized] div[role="button"]'
    );

    if (marketPlaceElement && !marketPlaceElement.getAttribute('do-not-click')) {
        TriggerClickEvent(marketPlaceElement);
        marketPlaceElement.setAttribute('do-not-click', 'true');
    }

    if (currentUrl.includes(expectedUrl)) {
        console.log("already correct chat opened");
        return;
    }

    let chatElement = document.querySelector(
        `a[href*="/messages/t/${fbChatId}/"]`
    );

    //setting the current input field text as current chat id. before navigation.
    var inputField = document.querySelector(".notranslate");
    HandleTextMessage(inputField, currentChatId);

    if (chatElement) {
        TriggerClickEvent(chatElement);
    }
    else {
        navigateToChat(fbChatId);
    }

    var isReady = await waitForCorrectChatInputField(fbChatId, 10000);

    try {
        await waitForUrl(expectedUrl, 5000);
    }
    catch (ex) {
        debugger;
    }

    return true;
}

async function waitForCorrectChatInputField(chatId, timeout = 10000) {
    const startTime = Date.now();

    while (Date.now() - startTime < timeout) {
        const inputField = document.querySelector(".notranslate");

        //if we are on chat id 1, and wanted to navigate to chat id 2, we are first setting text as 1 then navigating to 2
        //in this case we are checking inputField.textContent === '' but in case later we need to navigate to chat id 1,
        //its text would not be empty but set to 1, because we are setting the text beforing navigating to its id.
        if (inputField && (inputField.textContent.trim() === '' || inputField.textContent.trim() === chatId.trim())) {
            return true;
        }

        // Wait 100ms before checking again
        await new Promise(resolve => setTimeout(resolve, 100));
    }

    console.error('Timeout: Input field not available or not empty');
    return false;
}

function navigateToChat(chatId) {
    const url = `/messages/t/${chatId}/`;

    // Update the URL
    window.history.pushState({}, '', url);

    // Trigger popstate event so React Router detects the change
    window.dispatchEvent(new PopStateEvent('popstate', { state: {} }));
}

async function waitForUrl(urlPattern, timeout = 30000) {
    return new Promise((resolve, reject) => {
        const startTime = Date.now();

        const checkUrl = setInterval(() => {
            if (window.location.href.includes(urlPattern)) {
                clearInterval(checkUrl);
                resolve(window.location.href);
            }

            if (Date.now() - startTime > timeout) {
                clearInterval(checkUrl);
                //console.log(`Timeout waiting for URL: ${urlPattern}`);
                reject(new Error(`Timeout waiting for URL: ${urlPattern}`));
            }

            console.log("waiting for chat url");
        }, 100);
    });
}

//this will return fbListingTitle and fbListingImg only.
function GetListingInfo(fbChatId) {
    let anchorElement = document.querySelector(
        `a[href*="/messages/t/${fbChatId}/"]`
    );

    //console.log("Anchor element:", anchorElement);

    let fbListingTitle = anchorElement?.querySelector(
        'span[dir="auto"] > span'
    ).textContent;

    let imageDiv = anchorElement?.querySelector("div[data-visualcompletion]");
    let fbListingImg = imageDiv?.querySelector("img")?.src;

    //console.log("Fb listing title :", fbListingTitle);
    //console.log("Fb listing image :", fbListingImg);

    return { fbListingTitle, fbListingImg };
}

//this will return fbListingId and the user profile logo
function GetListingDetails(fbListingTitle) {
    let fbListingId = document.querySelector("a[href*='/item']")?.href;
    let match = fbListingId?.match(/\item\/(\d+)/);
    fbListingId = match ? match[1] : null;

    let messagesContainer = document.querySelector(
        `div[aria-label*="Messages in conversation titled ${fbListingTitle ?? ""
        }" i]`
    );
    let userProfileImg = messagesContainer?.querySelector(
        'img[style*="border-radius: 50%"]'
    )?.src;

    //console.log("User profile image: ", userProfileImg);
    //console.log("Fb listing Id", fbListingId);

    return { fbListingId, userProfileImg };
}

const waitForElement = (selector, timeout = 5000) => {
    //console.log("Timer:", timeout);
    return new Promise((resolve, reject) => {
        if (document.querySelector(selector)) {
            console.log("elemet found instantly.");
            return resolve(document.querySelector(selector));
        }

        const observer = new MutationObserver(() => {
            if (document.querySelector(selector)) {
                //console.log("elemet found after some time.");
                observer.disconnect();
                return resolve(document.querySelector(selector));
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true,
        });

        setTimeout(() => {
            observer.disconnect();
            resolve(new Error(`Element ${selector} not found within ${timeout}ms`));
        }, timeout);
    });
};

function GetTrimmedFbListingTitle(fbListingTitle) {
    let trimmedTitle = "";
    if (fbListingTitle?.length > 30) {
        for (let i = 0; i < 30; i++) {
            trimmedTitle += fbListingTitle[i];
        }
        return trimmedTitle;
        console.log("Trimmed Title :", trimmedTitle);
    }

    return fbListingTitle;
}

function delay(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

function checkAccountAuth() {
    let previousLoginState = null;

    function checkAndNotify() {
        var isLoggedIn = isAccountLoggedIn(getCookie('c_user'), getEmailInput());

        // Notify if state changed OR first time check
        if (previousLoginState !== isLoggedIn) {
            NotifyAccountAuthStatus(isLoggedIn);
            previousLoginState = isLoggedIn;
        }
    }

    // first run
    setTimeout(checkAndNotify, 1000);
    // then every 5 minutes
    setInterval(checkAndNotify, 20 * 60 * 1000);

    //cleans pendingMessages every 5 minutes..
    setInterval(() => {
        const fiveMinutesAgo = Date.now() - (5 * 60 * 1000);
        const before = pendingMessages.length;
        pendingMessages = pendingMessages.filter(msg => msg.timestamp > fiveMinutesAgo);
        const removed = before - pendingMessages.length;
        if (removed > 0) console.log(`Cleaned up ${removed} stale pending messages.`);
    }, 60 * 1000); // runs every minute
}

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
};
function getEmailInput() {
    return document.getElementById('email');
}
function isAccountLoggedIn(cUser, emailInput) {
    var loggedIn = false;

    if (cUser) {
        loggedIn = true;
    }

    if (emailInput) {
        loggedIn = false;
    }

    return loggedIn;
}

var userId;

function extractUserId() {
    if (userId) {
        return userId; // Return cached userId if already extracted
    }

    var pageSource = document.documentElement.innerHTML;

    try {
        const match = pageSource.match(/"USER_ID":"(\d+)"/);
        if (match) {
            userId = match[1];
            return userId;
        }
    } catch (e) {
        console.error("Error extracting USER_ID:", e);
    }
    return null;
}

function NotifyAccountAuthStatus(isLoggedIn) {
    var root = document.documentElement;
    let reason = isLoggedIn ? accountReason.AssignedToLocalServer : accountReason.ExpiredOrInvalidCookie;
    let authStatus = isLoggedIn ? accountAuthStatus.LoggedIn : accountAuthStatus.LoggedOut;
    let connectionStatus = isLoggedIn ? accountConnectionStatus.Online : accountConnectionStatus.Offline;

    console.log("Informing Account auth status to local server");
    console.log("Account Auth status is  :", authStatus);
    console.log("Account Conenction status is  :", connectionStatus);


    let detail = {
        fbAccountId: extractUserId(),
        accountAuthStatus: authStatus,
        accountConnectionStatus: connectionStatus,
        reason,
        isLoggedIn,
    }
    root.dispatchEvent(
        new CustomEvent("notifyAccountAuthState", {
            detail,
        })
    );
}

async function SyncListingInfo(fbChatId, chatId) {

    await NavigateToRequestedChat(fbChatId);

    let listingInfo = GetListingInfo(fbChatId);
    let fbListingTitle = listingInfo.fbListingTitle;
    let fbListingImg = listingInfo.fbListingImg;

    console.log("Before Trimming:", fbListingTitle);
    //Will give us a trimmed title of 30 char max as we need it below in waitForElement so we can get the element properly.
    fbListingTitle = GetTrimmedFbListingTitle(fbListingTitle);
    console.log("After Trimming:", fbListingTitle);

    //wait for the chat messages to load so we can get logo of the person who has send us the message this function will wait max of 4 seconds if image is found than returns immedailty .
    try {
        await waitForElement(
            `div[aria-label*='Messages in conversation titled ${fbListingTitle}' i]`,
            4000
        );
    } catch (err) {
        //console.error('4 sec issue');
        //silently move forward if the timeout exceed and still dom doesnot loaded so we move forward.
    }

    //will return us only fbListingId if that div is being shown by the fb as it sometime comes and sometimes does not, and the userprofile image.
    var listingDetails = GetListingDetails(fbListingTitle);
    let fbListingId = listingDetails.fbListingId;
    let userProfileImg = listingDetails.userProfileImg;

    var data = {
        fbListingTitle,
        fbListingImg,
        fbListingId,
        userProfileImg,
        chatId
    }

    var root = document.documentElement;

    root.dispatchEvent(
        new CustomEvent("syncListingDetail", {
            detail: data,
        })
    );
}


function CloseFbChatRecoverPopup() {
    const totalTriesToCloseFbChatRecoverPopup = 20;
    let attemptedTries = 0;
    let closeBtn;
    let dontRestoreButton;
    let timeoutId;

    const intervalId = setInterval(() => {
        attemptedTries++;
        if (attemptedTries >= totalTriesToCloseFbChatRecoverPopup) {
            clearInterval(intervalId);
            if (timeoutId) {
                clearTimeout(timeoutId);
            }
            return;
        }

        if (!closeBtn) {
            closeBtn = document.querySelector(
                'div[role="dialog"] div[aria-label="Close"][role="button"]'
            );
            // console.log("Close button found?", !!closeBtn);
        }

        if (closeBtn) {
            console.log("Clicking close button...");
            TriggerClickEvent(closeBtn);

            if (timeoutId) {
                clearTimeout(timeoutId);
            }

            timeoutId = setTimeout(() => {
                if (!dontRestoreButton) {
                    dontRestoreButton = document.querySelectorAll(
                        'div[role="button"][aria-label="Don\'t restore messages"]'
                    )[1];
                }
                console.log(dontRestoreButton);
                // console.log("Don't restore button found?", !!dontRestoreButton);

                if (dontRestoreButton) {
                    //console.log("SUCCESS - Clicking don't restore and stopping!");
                    TriggerClickEvent(dontRestoreButton);
                    clearInterval(intervalId);
                    clearTimeout(timeoutId);
                }
            }, 500);
        }
    }, 2000);
}


async function ScrollSideBarToLoadChats() {

    await waitForElement('div[aria-label="Chats"][role="grid"] [data-virtualized] div[role="button"]', 10000);

    const marketPlaceElement = document.querySelector('div[aria-label="Chats"][role="grid"] [data-virtualized] div[role="button"]');

    if (marketPlaceElement && !marketPlaceElement.getAttribute('do-not-click')) {
        TriggerClickEvent(marketPlaceElement);
        marketPlaceElement.setAttribute('do-not-click', 'true');
    }

    await waitForElement('div[aria-label="Marketplace"][role="grid"]', 10000);

    const parent = document.querySelector('div[aria-label="Marketplace"][role="grid"]');
    const scrollableDiv = findFirstScrollableElement(parent);

    if (!scrollableDiv) {
        console.log('Scrollable div not found!');
        return;
    }

    const fiveMinutes = 5 * 60 * 1000; // 5 minutes in milliseconds
    const startTime = Date.now();

    // This runs every 50ms and scrolls DOWN
    const interval = setInterval(() => {
        // Check if 5 minutes passed
        if (Date.now() - startTime >= fiveMinutes) {
            clearInterval(interval);
            console.log('5 minutes completed!');
            scrollableDiv.scrollTop = 0;
            return;
        }
        scrollableDiv.scrollTop += 100;  // This scrolls DOWN 100 pixels every 50ms

    }, 50); // Runs every 50 milliseconds
}

function findFirstScrollableElement(parent) {
    if (!parent) return null;

    const elements = parent.querySelectorAll('div');

    for (const el of elements) {
        if (isElementScrollable(el)) {
            return el;
        }
    }

    return null;
}
function isElementScrollable(element) {
    const style = window.getComputedStyle(element);

    const hasOverflowProperty =
        style.overflow === 'auto' ||
        style.overflow === 'scroll' ||
        style.overflowY === 'auto' ||
        style.overflowY === 'scroll';

    return hasOverflowProperty;
}

setTimeout(() => {
    CloseFbChatRecoverPopup();
    checkAccountAuth();
    ScrollSideBarToLoadChats();
}, 1100);