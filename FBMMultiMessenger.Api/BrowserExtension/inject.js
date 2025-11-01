// Global FIFO queue - array of objects
let pendingMessages = [];

console.log("Hello world the file has been changed.");
console.log("Hello world the file has been changed twice.");

let messageQueue = [];
let isProcessingMessage = false;

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

                // Replace your current ArrayBuffer branch with this:
                if (data instanceof ArrayBuffer) {
                    const text = new TextDecoder().decode(new Uint8Array(data));
                    if (text.includes("otid")) {
                        HandlerSentMessage(text);
                    }
                }
            } catch (err) {
                console.error("Error processing sent message:", err);
            }

            return originalSend.apply(this, arguments);
        };


        // Listen for messages received from the WebSocket
        wsInstance.addEventListener("message", function (event) {
            let receivedData = event.data;

            if (receivedData instanceof Blob) {
                // Handle Blob messages
                const reader = new FileReader();
                reader.onload = function () {
                    try {
                        const text = new TextDecoder().decode(
                            new Uint8Array(reader.result)
                        );
                        if (text.includes("insertMessage")) {
                            handleInsertMessage(text);
                        }
                    } catch (error) {
                        console.log(
                            "WebSocket Received (Binary Blob):",
                            new Uint8Array(reader.result)
                        );
                    }
                };
                reader.readAsArrayBuffer(event.data);
            } else if (receivedData instanceof ArrayBuffer) {
                // Handle ArrayBuffer messages
                try {
                    const text = new TextDecoder().decode(new Uint8Array(receivedData));
                    if (text.includes("insertMessage")) {
                        handleInsertMessage(text);
                    }
                } catch (error) {
                    console.log(
                        "WebSocket Received (Binary ArrayBuffer):",
                        new Uint8Array(receivedData)
                    );
                }
            } else {
                // Handle plain text messages
                if (receivedData.includes("insertMessage")) {
                    handleInsertMessage(receivedData);
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
                data.payload.tasks = data.payload.tasks.map(task => {
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
                    const otidExists = pendingMessages.some(msg => msg.otid === task.payload.otid);

                    if (!otidExists) {
                        const pending = pendingMessages.find(msg => !msg.otid);
                        if (pending) {
                            pending.otid = task.payload.otid;
                        }
                    }
                }
            }
        }
        catch (error) {
        }
    }

    function extractJsonPayload(rawMessage) {
        const jsonStart = rawMessage.indexOf('{"request_id":');
        if (jsonStart !== -1) {
            return rawMessage.substring(jsonStart);
        } else {
            throw new Error("No JSON found in the message");
        }
    }

    // Function to handle 'insertMessage' and show notifications
    async function handleInsertMessage(messageData) {
        try {
            messageData = extractJsonPayload(messageData);
            console.log(`${messageData}`);
            // Parse the JSON data
            const data = JSON.parse(messageData);
            const sp = data.sp;

            //removeAllParticipantsForThread => means new chat started
            //applyAdminMessageCTA => means zubair started this chat
            //updateAttachmentCtaAtIndexIgnoringAuthority => means message option when first message receive on any listing, like "Is this available?", Sorry, its not available, etc.
            //moveThreadToInboxAndUpdateParent => means user send a message.

            //syncBumpThreadDataToClient => Profile to profile encrypted data. in this case insertMessage does not come in sp array. so it will not reach this point.
            const notifiers = [
                "removeAllParticipantsForThread",
                "applyAdminMessageCTA",
            ];

            const containsMatch = sp.some((value) => {
                notifiers.includes(value);
            });

            let IsSent = sp.some(
                (value) => value === "moveThreadToInboxAndUpdateParent"
            );

            //if this bit is coming it means that this is a legit message and not system message
            //for example  Zubair started the chat , Zubair named this group we don't need these messages
            //ONLY valid message including first initial message: Is this available?
            let isLegitUserMessage = sp.some(
                (value) => value === "checkAuthoritativeMessageExists"
            );

            if (containsMatch || !isLegitUserMessage) {
                console.warn("Returning back not calling our api.");
                return;
            }

            // Extract the message and chat ID
            const payload = JSON.parse(data.payload);

            var fbAccountId = extractUserId();

            let messages = [];
            let offlineUniqueId;
            let fbOTID;
            let messageText = findMessage(payload);

            if (IsSent) {
                //get the offline unique id by matching the text message
                var indexOfPendingMessages = pendingMessages.findIndex(msg => msg.otid && messageData.includes(msg.otid));
                if (indexOfPendingMessages !== -1) {
                    const removed = pendingMessages.splice(indexOfPendingMessages, 1)[0];
                    offlineUniqueId = removed.uniqueId;
                    fbOTID = removed.otid;
                }
            }

            if (!fbOTID) {
                //if customer sends message or we have two tabs opened and we send a message. this will help in both cases.
                fbOTID = extractOTIDFromReceivedMessage(messageData);
            }

            if (!offlineUniqueId) {
            }

            messages.push(messageText);

            const mediaResult = extractMediaUrls(messageData);

            console.log("Media Result:", mediaResult);

            const fbChatId = extractChatId(messageData);

            var IsImageMessage = mediaResult.hasImages && !mediaResult.hasVideos;
            var IsVideoMessage = !IsImageMessage && mediaResult.hasVideos;
            var IsAudioMessage = mediaResult.hasAudio;
            var IsTextMessage = !IsImageMessage && !IsVideoMessage && !IsAudioMessage;

            if (IsImageMessage) {
                messages = mediaResult.images;
            } else if (IsVideoMessage) {
                messages = mediaResult.videos;
            } else if (IsAudioMessage) {
                messages = mediaResult.audio;
            }

            //Navigate to requested chat only if we are receiving a message because we are sending message we already be on the right chat as iam navigating there as well
            if (!IsSent) {
                await NavigateToRequestedChat(fbChatId);
            }

            //if we navigate to requested chat wait for 2 seconds so the (sidebar) requested chat will be rendered in the dom.
            try {
                await waitForElement(`a[href*="/messages/t/${fbChatId}/"]`, 2000);
            } catch (err) {
                console.error('2 sec issue');
                //silently move forward if the timeout exceed and still dom doesnot loaded so we move forward.
            }

            let listingInfo = GetListingInfo(fbChatId);
            let fbListingTitle = listingInfo.fbListingTitle;
            let fbListingImg = listingInfo.fbListingImg;

            //Will give us a trimmed title of 30 char max as we need it below in waitForElement so we can get the element properly.
            fbListingTitle = GetTrimmedFbListingTitle(fbListingTitle);

            //wait for the chat messages to load so we can get logo of the person who has send us the message this function will wait max of 4 seconds if image is found than returns immedailty .
            try {
                await waitForElement(
                    `div[aria-label*='Messages in conversation titled ${fbListingTitle}' i]`,
                    4000
                );
            } catch (err) {
                console.error('4 sec issue');
                //silently move forward if the timeout exceed and still dom doesnot loaded so we move forward.
            }

            //will return us only fbListingId if that div is being shown by the fb as it sometime comes and the userprofile image.
            var listingDetails = GetListingDetails(fbListingTitle);
            let fbListingId = listingDetails.fbListingId;
            let userProfileImg = listingDetails.userProfileImg;

            MakeAjaxCall(
                fbAccountId,
                fbChatId,
                fbListingTitle,
                fbListingImg,
                fbListingId,
                userProfileImg,
                messages,
                offlineUniqueId,
                IsTextMessage,
                IsImageMessage,
                IsVideoMessage,
                IsAudioMessage,
                !IsSent,
                fbOTID
            );

        } catch (error) {
            console.error("Error parsing message:", error);
        }
    }

    async function MakeAjaxCall(
        fbAccountId,
        fbChatId,
        fbListingTitle,
        fbListingImg,
        fbListingId,
        userProfileImg,
        messages,
        offlineUniqueId,
        IsTextMessage,
        IsImageMessage,
        IsVideoMessage,
        IsAudioMessage,
        IsReceived,
        fbOTID
    ) {
        var root = document.documentElement;
        var data = {
            fbAccountId,
            fbChatId,
            fbListingImg,
            fbListingTitle,
            fbListingId,
            userProfileImg,
            messages,
            offlineUniqueId,
            IsTextMessage,
            IsImageMessage,
            IsVideoMessage,
            IsAudioMessage,
            IsReceived,
            fbOTID
        };

        console.log("sending data to content.js", data);

        root.dispatchEvent(
            new CustomEvent("sendMessageToApi", {
                detail: data
            })
        );
    }

    function extractOTIDFromReceivedMessage(messageData) {
        try {
            const parsed = JSON.parse(messageData);
            const payload = JSON.parse(parsed.payload);

            // Navigate through the nested structure to find checkAuthoritativeMessageExists
            if (payload.step && Array.isArray(payload.step)) {
                const findOTID = (arr) => {
                    for (const item of arr) {
                        if (Array.isArray(item)) {
                            // Look for: [5, "checkAuthoritativeMessageExists", [19, "someid"], "OTID"]
                            if (item[0] === 5 && item[1] === "checkAuthoritativeMessageExists" && item[3]) {
                                return item[3]; // This is the OTID
                            }
                            // Recursively search nested arrays
                            const result = findOTID(item);
                            if (result) return result;
                        }
                    }
                    return null;
                };

                return findOTID(payload.step);
            }
        } catch (err) {
            console.error("Failed to extract OTID:", err);
        }
        return null;
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

    function findMessage(obj) {
        if (Array.isArray(obj)) {
            for (let item of obj) {
                // Check for matching structure: [action, "insertMessage", message]
                if (
                    Array.isArray(item) &&
                    item[0] === 5 &&
                    item[1] === "insertMessage" &&
                    item[2]
                ) {
                    return item[2]; // The message is in the 3rd position (index 2)
                }
                const result = findMessage(item); // Recursively search through nested arrays
                if (result) return result;
            }
        } else if (typeof obj === "object" && obj !== null) {
            for (let key in obj) {
                const result = findMessage(obj[key]); // Recursively search object properties
                if (result) return result;
            }
        }
        return null;
    }

    function extractChatId(rawPayload) {
        try {
            const parsed = JSON.parse(rawPayload);
            const innerPayload = parsed.payload
                .replace(/\\\//g, "/")
                .replace(/\\"/g, '"');

            // 1️⃣ Try context-based (most accurate if marker exists)
            const contextRegex = /checkAuthoritativeMessageExists",\[19,"(\d+)"\]/;
            const contextMatch = innerPayload.match(contextRegex);
            if (contextMatch) {
                return contextMatch[1];
            }

            // 2️⃣ Fallback: find all numeric IDs
            const genericRegex = /\[19,"(\d+)"\]/g;
            let match;
            const freqMap = {};

            while ((match = genericRegex.exec(innerPayload)) !== null) {
                const id = match[1];
                freqMap[id] = (freqMap[id] || 0) + 1;
            }

            // 3️⃣ Pick the one that is longest & most frequent
            let bestId = null;
            let bestScore = -1;

            for (const id in freqMap) {
                const score = id.length * freqMap[id];
                if (score > bestScore) {
                    bestScore = score;
                    bestId = id;
                }
            }

            return bestId;
        } catch (err) {
            console.error("extractChatId failed:", err);
            return null;
        }
    }

    // For images, videos & voice messages
    function extractMediaUrls(rawPayload) {
        // Step 1: parse the outer JSON
        const parsed = JSON.parse(rawPayload);

        // Step 2: get inner payload (escaped JSON string)
        const innerPayload = parsed.payload;

        // Step 3: clean up escape sequences
        const cleanString = innerPayload.replace(/\\\//g, "/").replace(/\\"/g, '"');

        let imageUrls = cleanString.match(
            /https:\/\/scontent[^" ]+\.(?:png|jpg|jpeg|webp|gif)[^" ]*/gi
        );

        //excluding thumbnails with stp= parameter => as we were getting multiple same images this would get the job done and will return only the image we needed.
        if (imageUrls) {
            imageUrls = imageUrls.filter((url) => !url.includes("stp="));
        }

        // Step 5: match video URLs
        let videoUrls = cleanString.match(
            /https:\/\/scontent[^" ]+\.(?:mp4|mov|avi|mkv|webm)[^" ]*/gi
        );

        // Step 6: match audio URLs (Facebook audio messages)
        let audioUrls = cleanString.match(
            /https:\/\/cdn\.fbsbx\.com[^" ]+\.mp4[^" ]*/gi
        );

        // Step 7: also match general audio formats
        let generalAudioUrls = cleanString.match(
            /https:\/\/[^" ]+\.(?:mp3|wav|m4a|aac|ogg|flac)[^" ]*/gi
        );

        // Combine audio URLs
        let allAudioUrls = [...(audioUrls || []), ...(generalAudioUrls || [])];

        // Remove duplicate if exist
        const unique = (arr) => [...new Set(arr)];

        imageUrls = unique(imageUrls);
        videoUrls = unique(videoUrls);
        allAudioUrls = unique(allAudioUrls);

        // Step 8: build response
        return {
            hasImages: !!(imageUrls && imageUrls.length),
            images: imageUrls || [],

            hasVideos: !!(videoUrls && videoUrls.length),
            videos: videoUrls || [],

            hasAudio: !!(allAudioUrls && allAudioUrls.length),
            audio: allAudioUrls || [],
        };
    }

    // Restore original properties and methods
    window.WebSocket.prototype = originalWebSocket.prototype;
})();

window.addEventListener("message", (event) => {
    // Make sure it’s from our own extension, not other scripts
    if (event.source !== window) return;

    if (event.data.type === "Persist_Message_From_App") {
    }

    if (event.data.type === "SET_FACEBOOK_MESSAGE") {
        messageQueue.push({
            messageData: event.data.data,
            defaultTemplate: event.data.defaultTemplate
        });

        // Start processing queue
        processMessageQueue();
    }
});


async function processMessage(messageData, defaultTemplate, fbChatId) {
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
                console.log("Message interval id:", textMessageInputInterval);
                clearInterval(textMessageInputInterval);
                console.log("Input found:", input);

                var textMessage = messageData.message;
                let offlineUniqueId = messageData.offlineUniqueId;

                try {
                    pendingMessages.push({
                        uniqueId: offlineUniqueId,
                        timestamp: Date.now()
                    });
                } catch (err) {
                }

                if (textMessage) {
                    HandleTextMessage(input, textMessage, defaultTemplate);
                }

                const mediaBase64s = messageData.mediaBase64;
                mediaBase64s.forEach((mediaBase64) => {
                    const blob = Base64T0Blob(mediaBase64);
                    const imageFile = new File([blob], "image.jpg", {
                        type: "image/jpeg",
                    });
                    uploadImage(imageFile);
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
                console.log(`Attempting to find input field... attempt ${attempts} of ${maxAttempts}`);
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
        const { messageData, defaultTemplate } = messageQueue.shift();
        await processMessage(messageData, defaultTemplate);
    }

    isProcessingMessage = false;
}

function HandleTextMessage(input, message, defaultTemplate) {
    if (!input || !input.__lexicalEditor) {
        console.log("Input not found where the text should be inserted.");
        return false;
    }

    var jsonMessageTemplate = JSON.parse(defaultTemplate);

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

    if (currentUrl.includes(expectedUrl)) {
        console.log('already correct chat opened');
        return;
    }

    const marketPlaceElement = document.querySelector(
        'div[aria-label="Chats"][role="grid"] [data-virtualized] div[role="button"]'
    );

    if (marketPlaceElement) {
        TriggerClickEvent(marketPlaceElement);
    }


    let chatElement = document.querySelector(
        `a[href*="/messages/t/${fbChatId}/"]`
    );

    if (chatElement) {
        TriggerClickEvent(chatElement);
    }

    await waitForUrl(expectedUrl, 5000);

    return true;
}

async function waitForUrl(urlPattern, timeout = 30000) {
    return new Promise((resolve, reject) => {
        const startTime = Date.now();

        const checkUrl = setInterval(() => {
            if (window.location.href.includes(urlPattern)) {
                clearInterval(checkUrl);
                resolve(window.location.href);
            }

            // Timeout
            if (Date.now() - startTime > timeout) {
                clearInterval(checkUrl);
                console.log(`Timeout waiting for URL: ${urlPattern}`);
                reject(new Error(`Timeout waiting for URL: ${urlPattern}`));
            }

            console.log('waiting for chat url');

        }, 100); // Check every 100ms
    });
}

//this will return fbListingTitle and fbListingImg only.
function GetListingInfo(fbChatId) {
    let anchorElement = document.querySelector(
        `a[href*="/messages/t/${fbChatId}/"]`
    );

    let fbListingTitle = anchorElement?.querySelector(
        'span[dir="auto"] > span'
    ).textContent;

    let imageDiv = anchorElement?.querySelector("div[data-visualcompletion]");
    let fbListingImg = imageDiv?.querySelector("img")?.src;

    console.log("Fb listing title :", fbListingTitle);
    console.log("Fb listing image :", fbListingImg);

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

    console.log("User profile image: ", userProfileImg);
    console.log("Fb listing Id", fbListingId);

    return { fbListingId, userProfileImg };
}

const waitForElement = (selector, timeout = 5000) => {
    console.log("Timer:", timeout);
    return new Promise((resolve, reject) => {
        if (document.querySelector(selector)) {
            console.log("elemet found instantly.");
            return resolve(document.querySelector(selector));
        }

        const observer = new MutationObserver(() => {
            if (document.querySelector(selector)) {
                console.log("elemet found after some time.");
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
    let trimmedTitle;
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
    return new Promise(resolve => setTimeout(resolve, ms));
}


//TODO: CLOSE POPUP
(function CloseFbChatRecoverPopup() {
    const totalTriesToCloseFbChatRecoverPopup = 100;
    let attemptedTries = 0;
    let closeBtn;

    const intervalId = setInterval(() => {
        attemptedTries++;

        if (!closeBtn) {
            closeBtn = document.querySelector(
                'div[role="dialog"] div[role="button"]'
            );
        }

        if (closeBtn) {
            TriggerClickEvent(closeBtn);
            setTimeout(() => {
                let dontRestoreButton = document.querySelectorAll(
                    'div[role="button"][aria-label="Don\'t restore messages"]'
                )[1];

                if (dontRestoreButton) {
                    TriggerClickEvent(dontRestoreButton);
                    clearInterval(intervalId);
                }
            }, 500);
        }

        if (attemptedTries >= totalTriesToCloseFbChatRecoverPopup) {
            clearInterval(intervalId);
        }
    }, 2000);
})();
