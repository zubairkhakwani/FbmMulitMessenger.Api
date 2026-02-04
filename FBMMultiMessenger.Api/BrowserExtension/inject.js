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

// Global FIFO queue - array of objects
let pendingMessages = [];

let messageQueue = [];
let isProcessingMessage = false;
let isNewChatStarted = false;

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
                            "Received (Binary Blob):",
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
                    else if (text.includes("insertNewMessageRange")) {
                        syncExistingMessages(text);
                    }
                } catch (error) {
                    console.log(
                        "(Binary ArrayBuffer):",
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

    function extractJsonPayload(rawMessage) {
        const jsonStart = rawMessage.indexOf('{"request_id":');
        if (jsonStart !== -1) {
            return rawMessage.substring(jsonStart);
        } else {
            throw new Error("No JSON found in the message");
        }
    }

    async function syncExistingMessages(messageData) {
        try {
            messageData = extractJsonPayload(messageData);

            // Parse the JSON data
            const data = JSON.parse(messageData);

            var fbAccountId = extractUserId();

            const parser = new MessengerPayloadParser();
            // Extract the messages and chat ID
            // Pass the payload STRING directly (not parsed JSON)
            var chats = parser.parsePayload(data.payload, fbAccountId); // Pass the STRING

            var apiRequest = {
                fbAccountId,
                accountId,
                chats
            };

            var root = document.documentElement;

            root.dispatchEvent(
                new CustomEvent("syncMessagesToApi", {
                    detail: apiRequest,
                })
            );
        } catch (error) {
            console.error("Error parsing message:", error);
        }
    }

    // Function to handle 'insertMessage' and show notifications
    async function handleInsertMessage(messageData) {
        try {
            messageData = extractJsonPayload(messageData);
            //console.log(`${messageData}`);
            // Parse the JSON data
            const data = JSON.parse(messageData);
            const sp = data.sp;

            //removeAllParticipantsForThread => means new chat started
            //applyAdminMessageCTA => means zubair started this chat
            //updateAttachmentCtaAtIndexIgnoringAuthority => means message option when first message receive on any listing, like "Is this available?", Sorry, its not available, etc.
            //moveThreadToInboxAndUpdateParent => means user send a message.

            //syncBumpThreadDataToClient => Profile to profile encrypted data. in this case insertMessage does not come in sp array. so it will not reach this point.

            //This will determine whether we received a message or we sent a message.
            let IsSent = sp.some(
                (value) => value === "moveThreadToInboxAndUpdateParent"
            );

            //if this bit is coming it means that this is a legit message and not a system message
            //for example  "Zubair started the chat" , "Zubair named this group" so we don't need these messages
            //ONLY valid message including first initial message: "Is this available?"
            let isLegitUserMessage = sp.some(
                (value) => value === "checkAuthoritativeMessageExists"
            );

            //When new chat will start we will get this sp "removeAllParticipantsForThread"
            //and for the next messages this sp will not be coming in the payload
            //so check for the first time if value is false and if it true we never set back to false as this will break the context.
            //we will only make this bit to false once we have send the default message.
            if (!isNewChatStarted) {
                isNewChatStarted = sp.some(
                    (value) => value === "removeAllParticipantsForThread"
                );
            }

            if (!isLegitUserMessage) {
                console.warn("Returning back not calling our api.");
                return;
            }

            // Extract the message and chat ID
            const payload = JSON.parse(data.payload);

            var fbAccountId = extractUserId();

            let messages = [];
            let offlineUniqueId;
            let fbOTID;
            let fbMessageId;
            let fbMessageReplyId;
            let timeStamp;
            let messageText = findMessage(payload);

            if (IsSent) {
                //get the offline unique id by matching the text message
                var indexOfPendingMessages = pendingMessages.findIndex(
                    (msg) => msg.otid && messageData.includes(msg.otid)
                );
                if (indexOfPendingMessages !== -1) {
                    const removed = pendingMessages.splice(indexOfPendingMessages, 1)[0];
                    offlineUniqueId = removed.uniqueId;
                    fbOTID = removed.otid;
                    fbMessageReplyId = removed.fbMessageReplyId;
                }
            }

            var otherMessageData = extractAllMessageData(messageData);
            fbMessageId = otherMessageData.messageId;
            timeStamp = otherMessageData.timestamp;

            if (!fbMessageReplyId) {
                fbMessageReplyId = otherMessageData.fbMessageReplyId;
            }

            if (!fbOTID) {
                //if customer sends message or we have two tabs opened and we send a message. this will help in both cases.
                fbOTID = extractOTIDFromReceivedMessage(messageData);
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

            //Navigate to requested chat only if we are receiving a message because if we are sending message we will already be on the right chat
            if (!IsSent) {
                await NavigateToRequestedChat(fbChatId);
            }

            console.log("New Chat started: ", isNewChatStarted);
            console.log("Default Message:", defaultMessage);

            if (isNewChatStarted && defaultMessage) {
                let input = document.querySelector(".notranslate");
                HandleTextMessage(input, defaultMessage);

                setTimeout(() => {
                    TriggerEnterEvent(input);
                }, 1000);

                isNewChatStarted = false;
            }

            //if we navigate to requested chat wait for 2 seconds so the (sidebar) requested chat will be rendered in the dom.
            try {
                await waitForElement(`a[href*="/messages/t/${fbChatId}/"]`, 2000);
            } catch (err) {
                //console.error('2 sec issue');
                //silently move forward if the timeout exceed and still dom doesnot loaded so we move forward.
            }

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

            //will return us only fbListingId if that div is being shown by the fb as it sometime comes and the userprofile image.
            var listingDetails = GetListingDetails(fbListingTitle);
            let fbListingId = listingDetails.fbListingId;
            let userProfileImg = listingDetails.userProfileImg;

            MakeAjaxCall(
                accountId,
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
                fbOTID,
                fbMessageId,
                fbMessageReplyId,
                timeStamp
            );
        } catch (error) {
            console.error("Error parsing message:", error);
        }
    }

    async function MakeAjaxCall(
        accountId,
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
        fbOTID,
        fbMessageId,
        fbMessageReplyId,
        timestamp
    ) {
        var root = document.documentElement;
        var data = {
            accountId,
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
            fbOTID,
            fbMessageId,
            fbMessageReplyId,
            timestamp
        };

        console.log("sending data to content.js", data);

        root.dispatchEvent(
            new CustomEvent("sendMessageToApi", {
                detail: data,
            })
        );
    }

    function extractAllMessageData(messageData) {
        try {
            const parsed = JSON.parse(messageData);
            const payload = JSON.parse(parsed.payload);

            let result = {
                messageId: null,
                timestamp: null,
                threadId: null,
                messageText: null,
                fbMessageReplyId: null
            };

            if (payload.step && Array.isArray(payload.step)) {
                const extractData = (arr) => {
                    for (const item of arr) {
                        if (Array.isArray(item)) {
                            // Check for OTID
                            if (
                                item[0] === 5 &&
                                item[1] === "checkAuthoritativeMessageExists" &&
                                item[3]
                            ) {
                                result.otid = item[3];
                            }

                            // Check for insertMessage data
                            if (
                                item[0] === 5 &&
                                item[1] === "insertMessage"
                            ) {
                                if (typeof item[2] === 'string') {
                                    result.messageText = item[2];
                                }
                                if (Array.isArray(item[5]) && item[5][0] === 19) {
                                    result.threadId = item[5][1];
                                }
                                if (Array.isArray(item[7]) && item[7][0] === 19) {
                                    result.timestamp = item[7][1];
                                }
                                if (typeof item[10] === 'string' && item[10].startsWith('mid.$')) {
                                    result.messageId = item[10];
                                }
                                if (typeof item[25] === 'string' && item[25].startsWith('mid.$')) {
                                    result.fbMessageReplyId = item[25];
                                }
                            }

                            // Recursively search nested arrays
                            extractData(item);
                        }
                    }
                };

                extractData(payload.step);
            }

            if (!result.messageId || !result.messageText) {
                //ToDO:
                //capture sentry exception with messagedata variable so we can find out why.
            }


            return result;
        } catch (err) {
            console.error("Failed to extract message data:", err);
            return null;
        }
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
                            if (
                                item[0] === 5 &&
                                item[1] === "checkAuthoritativeMessageExists" &&
                                item[3]
                            ) {
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

        //identifies the actual/downloadable video
        if (videoUrls) {
            videoUrls = videoUrls.filter((url) => url.includes("dl=1"));
        }

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
});

//It will listen for messages from content.js
window.addEventListener("message", (event) => {
    // Make sure it’s from our own extension, not other scripts
    if (event.source !== window) return;

    if (event.data.type === "SET_DEFAULT_MESSAGE") {
        console.log("Previous default message:", defaultMessage);
        defaultMessage = event.data.data;
        console.log("New default message:", defaultMessage);
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
    setInterval(checkAndNotify, 300000);
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

    console.log("CUser: ", cUser);
    console.log("Email Input: ", emailInput);

    if (cUser) {
        loggedIn = true;
    }

    if (emailInput) {
        loggedIn = false;
    }

    console.log("Account Logged In: ", loggedIn)
    return loggedIn;
}

function NotifyAccountAuthStatus(isLoggedIn) {
    var root = document.documentElement;
    let authStatus = isLoggedIn ? accountAuthStatus.LoggedIn : accountAuthStatus.LoggedOut;
    let connectionStatus = isLoggedIn ? accountConnectionStatus.Online : accountConnectionStatus.Offline;

    console.log("Informing Account auth status to local server");
    console.log(" Account id is :", accountId);
    console.log("Account Auth status is  :", authStatus);
    console.log("Account Conenction status is  :", connectionStatus);


    let detail = {
        accountId,
        accountAuthStatus: authStatus,
        accountConnectionStatus: connectionStatus,
        isLoggedIn,
    }
    root.dispatchEvent(
        new CustomEvent("notifyAccountAuthState", {
            detail,
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


function PringLogs(logs) {
    console.log(logs);
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


class MessengerPayloadParser {
    /**
     * Parses the Facebook Messenger payload and returns structured chat data
     * @param {string} payloadString - The raw payload string from payload.payload
     * @returns {Array} Array of chat objects with messages and metadata
     */
    parsePayload(payloadString, currentUserId) {
        try {
            // First parse to get the outer structure
            const outerData = typeof payloadString === 'string'
                ? JSON.parse(payloadString)
                : payloadString;

            const chats = new Map();
            const contacts = new Map();

            // The actual data is in the step array
            const steps = outerData?.step || [];

            // Process each step
            for (const step of steps) {
                this.processStep(step, chats, contacts, currentUserId);
            }

            // Convert chats Map to array and format
            return this.formatChats(chats, contacts, currentUserId);

        } catch (error) {
            console.error('Error parsing payload:', error);
            return [];
        }
    }

    processStep(data, chats, contacts, currentUserId) {
        if (!Array.isArray(data)) return;

        for (const item of data) {
            if (Array.isArray(item)) {
                // Look for operation arrays: [5, "operationName", ...params]
                if (item.length >= 2 && item[0] === 5 && typeof item[1] === 'string') {
                    const opName = item[1];
                    const params = item.slice(2);

                    switch (opName) {
                        case 'deleteThenInsertThread':
                            this.processThread(params, chats);
                            break;
                        case 'addParticipantIdToGroupThread':
                            this.processParticipant(params, chats);
                            break;
                        case 'upsertMessage':
                            this.processMessage(params, chats, currentUserId);
                            break;
                        case 'insertBlobAttachment':
                            this.processAttachment(params, chats);
                            break;
                        case 'insertStickerAttachment':
                            this.processStickerAttachment(params, chats);
                            break;
                        case 'insertXmaAttachment':
                            this.processXmaAttachment(params, chats);
                            break;
                        case 'verifyContactRowExists':
                            this.processContact(params, contacts);
                            break;
                    }
                }

                // Recursively process nested arrays
                this.processStep(item, chats, contacts, currentUserId);
            }
        }
    }

    processThread(params, chats) {
        // params structure from deleteThenInsertThread:
        // [0] = timestamp, [1] = timestamp, [2] = snippet, [3] = title, [4] = image, ...
        // [6] = ?, [7] = threadId, ...

        const threadId = this.extractValue(params[7]);
        if (!threadId) return;

        if (!chats.has(threadId)) {
            chats.set(threadId, {
                fbChatId: threadId,
                otherUserId: null,
                otherUserName: null,
                otherUserProfilePicture: null,
                listingTitle: null,
                listingImage: null,
                messages: [],
                participants: [],
            });
        }

        const chat = chats.get(threadId);
        chat.listingTitle = params[3]; // Thread name (e.g., "Depressed · bar stool for sale")
        chat.listingImage = params[4]; // Thread image URL
    }

    processParticipant(params, chats) {
        // params: [0] = threadId, [1] = participantId, ...
        const threadId = this.extractValue(params[0]);
        const participantId = this.extractValue(params[1]);

        if (!threadId || !participantId) return;

        if (!chats.has(threadId)) {
            chats.set(threadId, {
                fbChatId: threadId,
                otherUserId: null,
                otherUserName: null,
                otherUserProfilePicture: null,
                listingTitle: null,
                listingImage: null,
                messages: [],
                participants: [],
            });
        }

        const chat = chats.get(threadId);
        if (!chat.participants.some(p => p.id === participantId)) {
            chat.participants.push({
                id: participantId,
                addedBy: params[6] || 'Unknown',
                isCreator: params[13] === true
            });
        }
    }

    processMessage(params, chats, currentUserId) {
        // upsertMessage params:
        // [0] = text, [1] = ?, [2] = ?, [3] = threadId, [4] = ?, [5] = timestamp, 
        // [6] = timestamp, [7] = ?, [8] = messageId, [9] = ?, [10] = senderId, ...

        const text = params[0];
        const threadId = this.extractValue(params[3]);
        const timestamp = this.extractValue(params[5]);
        const messageId = params[8];
        const senderId = this.extractValue(params[10]);

        if (params[12] === true) {
            //means this is a system message. e.g you started this chat. you have not replied to this message. etc
            return;
        }

        if (!threadId || !messageId) return;

        // Initialize chat if it doesn't exist
        if (!chats.has(threadId)) {
            chats.set(threadId, {
                fbChatId: threadId,
                otherUserId: null,
                otherUserName: null,
                otherUserProfilePicture: null,
                listingTitle: null,
                listingImage: null,
                messages: [],
                participants: [],
            });
        }

        const chat = chats.get(threadId);

        // Check if message already exists
        if (chat.messages.some(m => m.messageId === messageId)) {
            return;
        }

        if (typeof text === "string") {
            const message = {
                messageId,
                text: text || '',
                timestamp: parseInt(timestamp) || 0,
                senderId,
                IsReceived: senderId != currentUserId,
                attachments: [],
                type: text ? 'text' : 'attachment'
            };

            chat.messages.push(message);
        }
        else {
            //adding message here, but when processAttachment or processStickerAttachment or processXmaAttachment gonna run it will add the attachment to relevant message...
            const message = {
                messageId,
                text: null,
                timestamp: parseInt(timestamp) || 0,
                senderId,
                IsReceived: senderId != currentUserId,
                attachments: [],
                type: 'attachment'
            };

            chat.messages.push(message);
        }
    }

    processXmaAttachment(params, chats) {
        // insertXmaAttachment params for system messages:
        // [24] = threadId, [28] = messageId, [93] = xma_type

        const threadId = this.extractValue(params[25]);
        const messageId = this.extractValue(params[30]);

        if (threadId && messageId) {
            const chat = chats.get(threadId);
            if (chat) {
                chat.messages = chat.messages.filter(m => m.messageId !== messageId);
            }
        }
    }

    processStickerAttachment(params, chats) {
        // insertStickerAttachment params:
        // [0] = url, [1] = fallback, [2] = timestamp, [3] = mimeType,
        // [4] = previewUrl, [5] = previewFallback, [6] = previewTimestamp,
        // [7] = previewMimeType, [8] = timestamp, [9] = width, [10] = height,
        // [11] = ?, [12] = ?, [13] = description (e.g., "Like, thumbs up"),
        // [14] = threadId, [15] = ?, [16] = mimeType, [17] = timestamp,
        // [18] = messageId, [19] = stickerId, ...

        const url = params[0];
        const description = params[13];
        const threadId = this.extractValue(params[14]);
        const messageId = params[18];
        const stickerId = params[19];
        const width = this.extractValue(params[9]);
        const height = this.extractValue(params[10]);

        if (!threadId || !messageId) return;

        const chat = chats.get(threadId);
        if (!chat) return;

        // Find the message this sticker belongs to
        const message = chat.messages.find(m => m.messageId === messageId);
        if (message) {
            message.attachments.push(url);

            // Update message type if it has no text
            if (!message.text && message.attachments.length > 0) {
                message.type = 'sticker';
            }
        }
    }

    processAttachment(params, chats) {
        // insertBlobAttachment params:
        // [0] = attachmentId (e.g., "image-xxx"), [1] = size, [2] = ?, [3] = url,
        // [4] = fallback, [5] = timestamp, [6] = mimeType, [7] = ?, [8] = previewUrl,
        // [9] = previewFallback, [10] = previewTimestamp, [11] = previewMimeType,
        // [12] = ?, [13] = ?, [14] = width, [15] = height, ...
        // [19] = threadId, [20] = ?, [21] = ?, [22] = mimeType, [23] = timestamp, 
        // [24] = messageId, ...

        const attachmentId = params[0];
        const url = params[3];
        const previewUrl = params[8];
        const width = this.extractValue(params[14]);
        const height = this.extractValue(params[15]);
        const threadId = this.extractValue(params[27]);
        const messageId = params[32];

        if (!threadId || !messageId) return;

        const chat = chats.get(threadId);
        if (!chat) return;

        // Determine attachment type
        let attachmentType = 'file';
        if (attachmentId?.includes('image')) {
            attachmentType = 'image';
        } else if (attachmentId?.includes('video')) {
            attachmentType = 'video';
        }

        // Find the message this attachment belongs to
        const message = chat.messages.find(m => m.messageId === messageId);
        if (message) {
            message.attachments.push(url);

            // Update message type if it has no text
            if (!message.text && message.attachments.length > 0) {
                message.type = attachmentType;
            }
        }
        else {
            console.error("attachment found but no message to link with");
        }
    }

    processContact(params, contacts) {
        // verifyContactRowExists params:
        // [0] = contactId, [1] = ?, [2] = profileImage, [3] = name, ...

        const contactId = this.extractValue(params[0]);
        const profileImage = params[2];
        const name = params[3];

        if (!contactId) return;

        contacts.set(contactId, {
            id: contactId,
            name: name,
            profileImage: profileImage
        });
    }

    formatChats(chats, contacts, currentUserId) {
        const formattedChats = [];

        for (const [, chat] of chats) {
            // Sort messages by timestamp
            chat.messages.sort((a, b) => a.timestamp - b.timestamp);

            // Enrich participants with contact info
            chat.participants = chat.participants.map(p => ({
                ...p,
                ...(contacts.get(p.id) || {})
            }));

            var otherParticipantId = chat.participants.find(p => p.id !== currentUserId)?.id;
            //in case user is blocked by facebook or contact left the chat, it will not get added to participant, so we will get its id from messages.
            if (!otherParticipantId) {
                otherParticipantId = chat.messages.find(m => m.senderId != currentUserId)?.senderId;
            }
            const otherUserContact = otherParticipantId ? contacts.get(otherParticipantId) : null;

            // Add other user info to chat object
            chat.otherUserId = otherParticipantId;
            chat.otherUserName = otherUserContact?.name || null;
            chat.otherUserProfilePicture = otherUserContact?.profileImage || null;

            // Add sender info to each message
            chat.messages = chat.messages.map(msg => ({
                ...msg,
            }));

            formattedChats.push(chat);
        }

        // Sort chats by most recent message
        formattedChats.sort((a, b) => {
            const aTime = a.messages.length > 0
                ? a.messages[a.messages.length - 1].timestamp
                : 0;
            const bTime = b.messages.length > 0
                ? b.messages[b.messages.length - 1].timestamp
                : 0;
            return bTime - aTime;
        });

        return formattedChats;
    }

    extractValue(param) {
        if (!param) return null;

        // Handle array format like [19, "value"]
        if (Array.isArray(param) && param.length >= 2) {
            return param[1];
        }

        return param;
    }
}