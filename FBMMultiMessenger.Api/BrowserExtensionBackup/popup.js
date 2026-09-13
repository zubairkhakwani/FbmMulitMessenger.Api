const loadingView = document.getElementById('loadingView');
const loginView = document.getElementById('loginView');
const connectedView = document.getElementById('connectedView');
const statusDot = document.getElementById('statusDot');
const statusText = document.getElementById('statusText');
const errorMsg = document.getElementById('errorMsg');
const accountIdLabel = document.getElementById('accountIdLabel');
const loginBtn = document.getElementById('loginBtn');
const logoutBtn = document.getElementById('logoutBtn');
const registrationErrorMsg = document.getElementById('registrationErrorMsg');
const apiKeyInput = document.getElementById('apiKeyInput');
const saveApiKeyBtn = document.getElementById('saveApiKeyBtn');
const apiKeyErrorMsg = document.getElementById('apiKeyErrorMsg');
const toggleApiKeyBtn = document.getElementById('toggleApiKeyBtn');
let showApiKey = false;

// ── Update the dot & label ────────────────────────────────
function setStatus(isConnected) {
    if (isConnected) {
        statusDot.className = 'dot green';
        statusText.textContent = 'Connected';
    } else {
        statusDot.className = 'dot red';
        statusText.textContent = 'Disconnected';
    }
}

function applyStatusResponse(response) {
    if (!response) {
        return;
    }

    setStatus(response.isConnected);
    apiKeyInput.value = response.apiKey || '';
    accountIdLabel.textContent = response.accountId || 'Pending...';
    registrationErrorMsg.textContent = response.registrationError || '';
}

// ── Show the right view ───────────────────────────────────
function showView(view) {
    loadingView.classList.add('hidden');
    loginView.classList.add('hidden');
    connectedView.classList.add('hidden');
    view.classList.remove('hidden');
}

// ── On popup open: ask background for current status ─────
chrome.runtime.sendMessage({ key: 'getStatus' }, (response) => {
    if (!response) {
        showView(connectedView);
        return;
    }

    applyStatusResponse(response);
    showView(connectedView);
});

function updateApiKeyVisibility() {
    apiKeyInput.type = showApiKey ? 'text' : 'password';
    toggleApiKeyBtn.textContent = showApiKey ? '🙈' : '👁';
    toggleApiKeyBtn.title = showApiKey ? 'Hide API key' : 'Show API key';
}

toggleApiKeyBtn.addEventListener('click', () => {
    showApiKey = !showApiKey;
    updateApiKeyVisibility();
});

// ── Save API key (Robo prefill or user-entered) ──────────
saveApiKeyBtn.addEventListener('click', () => {
    const key = apiKeyInput.value.trim();

    if (!key) {
        apiKeyErrorMsg.textContent = 'Please enter an API key.';
        return;
    }

    saveApiKeyBtn.disabled = true;
    saveApiKeyBtn.textContent = 'Saving...';
    apiKeyErrorMsg.textContent = '';

    chrome.runtime.sendMessage(
        { key: 'loginToApi', username: key, password: null },
        (response) => {
            saveApiKeyBtn.disabled = false;
            saveApiKeyBtn.textContent = 'Save API Key';

            if (response?.success) {
                chrome.runtime.sendMessage({ key: 'getStatus' }, (res) => {
                    applyStatusResponse(res);
                });
            } else {
                apiKeyErrorMsg.textContent = 'Invalid API key. Please check and try again.';
            }
        }
    );
});

// ── Login button (email/password — unchanged) ─────────────
loginBtn.addEventListener('click', async () => {
    const username = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value.trim();

    if (!username || !password) {
        errorMsg.textContent = 'Please enter username and password.';
        return;
    }

    loginBtn.disabled = true;
    loginBtn.textContent = 'Logging in...';
    errorMsg.textContent = '';

    chrome.runtime.sendMessage(
        { key: 'loginToApi', username, password },
        (response) => {
            loginBtn.disabled = false;
            loginBtn.textContent = 'Login';

            if (response?.success) {
                chrome.runtime.sendMessage({ key: 'getStatus' }, (res) => {
                    applyStatusResponse(res);
                    showView(connectedView);
                });
            } else {
                errorMsg.textContent = 'Login failed. Check your credentials.';
            }
        }
    );
});

// ── Logout button ─────────────────────────────────────────
logoutBtn.addEventListener('click', () => {
    chrome.runtime.sendMessage({ key: 'logout' }, () => {
        chrome.runtime.sendMessage({ key: 'getStatus' }, (res) => {
            applyStatusResponse(res);
            setStatus(false);
            showView(connectedView);
        });
    });
});

// ── Listen for real-time status changes from background ───
chrome.runtime.onMessage.addListener((request) => {
    if (request.key === 'registrationError') {
        registrationErrorMsg.textContent = request.message || '';
    }

    if (request.key === 'statusChanged') {
        setStatus(request.isConnected);

        if (request.accountId) {
            accountIdLabel.textContent = request.accountId;
            registrationErrorMsg.textContent = '';
        }
    }
});
