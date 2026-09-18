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

// Debug: page (inject.js) asked to show the captured background logs. Fetch them from the
// background service worker and render them as a popup on this page.
root.addEventListener("fbmShowLogs", function () {
    chrome.runtime.sendMessage({ key: "getFbmLogs" }, function (resp) {
        renderFbmLogsOverlay((resp && resp.logs) || []);
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

    if (request.action === "showFbmLogs") {
        renderFbmLogsOverlay(request.logs || []);
        sendResponse({ success: true });
    }
});


// Renders the extension's captured background logs as a popup on the page (top frame only). DOM-based, so
// it works even on anti-detect browsers where the console output is muted.
function renderFbmLogsOverlay(logs) {
    if (window.top !== window) return;

    var existing = document.getElementById('fbm-logs-overlay');
    if (existing) existing.remove();

    var wrap = document.createElement('div');
    wrap.id = 'fbm-logs-overlay';
    wrap.style.cssText = 'position:fixed;inset:0;z-index:2147483647;background:rgba(0,0,0,.6);display:flex;align-items:center;justify-content:center;font-family:Segoe UI,Arial,sans-serif;';

    var box = document.createElement('div');
    box.style.cssText = 'width:760px;max-width:94vw;height:80vh;background:#1c1e21;color:#e4e6eb;border-radius:10px;display:flex;flex-direction:column;gap:8px;padding:12px;box-shadow:0 8px 30px rgba(0,0,0,.5);';

    var header = document.createElement('div');
    header.style.cssText = 'font-weight:600;';
    header.textContent = 'FBM Messenger — background logs (' + (logs ? logs.length : 0) + ')';

    var ta = document.createElement('textarea');
    ta.readOnly = true;
    ta.style.cssText = 'flex:1;width:100%;background:#111;color:#d7f7c8;border:1px solid #444;border-radius:6px;font-family:monospace;font-size:12px;white-space:pre;overflow:auto;';
    ta.value = (logs && logs.length) ? logs.join('\n') : '(no logs captured yet)';

    var bar = document.createElement('div');
    bar.style.cssText = 'display:flex;gap:8px;justify-content:flex-end;';

    function mkBtn(label) {
        var b = document.createElement('button');
        b.textContent = label;
        b.style.cssText = 'padding:6px 12px;border-radius:6px;border:1px solid #55585e;background:#3a3d42;color:#e4e6eb;cursor:pointer;';
        return b;
    }

    var copyBtn = mkBtn('Copy');
    copyBtn.addEventListener('click', function () { ta.select(); try { document.execCommand('copy'); } catch (e) { } });

    var closeBtn = mkBtn('Close');
    closeBtn.addEventListener('click', function () { wrap.remove(); });

    bar.appendChild(copyBtn);
    bar.appendChild(closeBtn);
    box.appendChild(header);
    box.appendChild(ta);
    box.appendChild(bar);
    wrap.appendChild(box);
    (document.body || document.documentElement).appendChild(wrap);
}


function setMessage(data) {
    window.postMessage(
        {
            type: "SET_FACEBOOK_MESSAGE",
            data: data,
        },

        "*"
    );
}
