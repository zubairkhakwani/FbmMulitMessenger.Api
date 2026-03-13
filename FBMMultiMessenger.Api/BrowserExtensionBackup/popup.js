const loadingView = document.getElementById('loadingView');
const loginView = document.getElementById('loginView');
const connectedView = document.getElementById('connectedView');
const statusDot = document.getElementById('statusDot');
const statusText = document.getElementById('statusText');
const errorMsg = document.getElementById('errorMsg');
const accountIdLabel = document.getElementById('accountIdLabel');
const loginBtn = document.getElementById('loginBtn');
const logoutBtn = document.getElementById('logoutBtn');

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
        showView(loginView);
        return;
    }

    setStatus(response.isConnected);

    if (response.authToken) {
        // Already logged in — show connected view
        accountIdLabel.textContent = response.accountId || 'Pending...';
        showView(connectedView);
    } else {
        // Not logged in — show login form
        showView(loginView);
    }
});

// ── Login button ──────────────────────────────────────────
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
                // Ask for fresh status after login
                chrome.runtime.sendMessage({ key: 'getStatus' }, (res) => {
                    setStatus(res?.isConnected || false);
                    accountIdLabel.textContent = res?.accountId || 'Pending...';
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
        setStatus(false);
        showView(loginView);
    });
});

// ── Listen for real-time status changes from background ───
chrome.runtime.onMessage.addListener((request) => {
    if (request.key === 'statusChanged') {
        setStatus(request.isConnected);

        // Update account id if available
        if (request.accountId) {
            accountIdLabel.textContent = request.accountId;
        }
    }
});