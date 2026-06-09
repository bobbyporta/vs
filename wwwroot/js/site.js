// ========== HELPER FUNCTION ==========
function escapeHtml(text) {
    if (!text) return '';
    var div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ========== TOAST NOTIFICATION ==========
function showToast(message, type) {
    // Remove existing toast if any
    var existingToast = document.querySelector('.toast-notification');
    if (existingToast) {
        existingToast.remove();
    }

    // Create new toast
    var toast = document.createElement('div');
    toast.className = 'toast-notification';

    if (type === 'error') {
        toast.classList.add('error');
        toast.innerHTML = '<i class="fas fa-exclamation-circle"></i> ' + message;
    } else if (type === 'warning') {
        toast.classList.add('warning');
        toast.innerHTML = '<i class="fas fa-exclamation-triangle"></i> ' + message;
    } else {
        toast.innerHTML = '<i class="fas fa-check-circle"></i> ' + message;
    }

    document.body.appendChild(toast);

    // Auto remove after 3 seconds
    setTimeout(function () {
        toast.classList.add('closing');
        setTimeout(function () {
            if (toast) toast.remove();
        }, 300);
    }, 3000);
}

// ========== DEACTIVATED NOTIFICATION MODAL ==========
function showDeactivatedModal() {
    var modal = document.getElementById('deactivatedNotificationModal');
    if (modal) {
        modal.classList.remove('closing');
        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
    }
}

// ========== SESSION STATUS CHECKER ==========
var sessionCheckInterval = null;

function startSessionChecker() {
    if (sessionCheckInterval) clearInterval(sessionCheckInterval);
    sessionCheckInterval = setInterval(function () {
        fetch('/Home/CheckSessionStatus')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data.isActive) {
                    clearInterval(sessionCheckInterval);
                    showDeactivatedModal();
                }
            })
            .catch(function (err) { console.log('Session check error:', err); });
    }, 5000);
}

// ========== COMMON MODAL SETUP LOGIC ==========
// ========== MODAL ANIMATION LIFECYCLE CONTROLLER ==========
// ========== COMMON MODAL ENGINE (DYNAMIC THEME MANAGER) ==========
function openModal(modalId, data) {
    var modal = document.getElementById(modalId);
    if (!modal) return;

    var prefix = modalId.replace('Modal', '');
    var fullNameSpan = document.getElementById(prefix + 'FullName');
    var usernameSpan = document.getElementById(prefix + 'Username');
    var roleSpan = document.getElementById(prefix + 'Role');
    var messageDiv = document.getElementById(prefix + 'Message');

    // Populate data fragments safely
    if (fullNameSpan) fullNameSpan.textContent = data.fullName || 'N/A';
    if (usernameSpan) usernameSpan.textContent = data.username || 'N/A';
    if (roleSpan) roleSpan.textContent = data.role || 'N/A';

    // Locate matching confirmation container references inside the targeted overlay box
    var headerEl = modal.querySelector('.deactivate-modal-header');
    var headerTitleEl = modal.querySelector('.deactivate-modal-header h3');
    var warningBoxEl = modal.querySelector('.deactivate-warning');
    var confirmBtnEl = modal.querySelector('.deactivate-btn-confirm');

    // Theme Config Dictionary Definitions
    var themeMap = {
        'deactivate': {
            title: 'DEACTIVATE USER',
            headerBg: '#e65100', // Deep Orange
            warningBg: '#fff3e0',
            warningBorder: '#e65100',
            msg: 'Are you sure you want to deactivate user <strong>' + escapeHtml(data.fullName || data.username) + '</strong>?'
        },
        'activate': {
            title: 'ACTIVATE USER',
            headerBg: '#2e7d32', // Forest Green
            warningBg: '#e8f5e9',
            warningBorder: '#2e7d32',
            msg: 'Are you sure you want to activate user <strong>' + escapeHtml(data.fullName || data.username) + '</strong>?'
        },
        'delete': {
            title: 'DELETE USER ACCOUNT',
            headerBg: '#c62828', // Crimson Red
            warningBg: '#ffebee',
            warningBorder: '#c62828',
            msg: 'Are you sure you want to <strong>DELETE</strong> user <strong>' + escapeHtml(data.fullName || data.username) + '</strong>? This action cannot be undone.'
        },
        'unlock': {
            title: 'UNLOCK USER ACCOUNT',
            headerBg: '#ff9800', // Amber Warning
            warningBg: '#fff8e1',
            warningBorder: '#ff9800',
            msg: 'Are you sure you want to unlock user <strong>' + escapeHtml(data.fullName || data.username) + '</strong>?'
        }
    };

    // Apply the active runtime theme properties if available
    var currentTheme = themeMap[prefix];
    if (currentTheme) {
        if (messageDiv) messageDiv.innerHTML = currentTheme.msg;
        if (headerTitleEl) headerTitleEl.textContent = currentTheme.title;
        if (headerEl) headerEl.style.backgroundColor = currentTheme.headerBg;

        if (warningBoxEl) {
            warningBoxEl.style.backgroundColor = currentTheme.warningBg;
            warningBoxEl.style.borderColor = currentTheme.warningBorder;
        }
        if (confirmBtnEl) {
            confirmBtnEl.style.backgroundColor = currentTheme.headerBg;
            confirmBtnEl.textContent = prefix.charAt(0).toUpperCase() + prefix.slice(1); // Capitalizes text automatically
        }
    }

    // Trigger smooth fade animations
    modal.classList.remove('closing');
    void modal.offsetWidth; // Repaint trigger bound
    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeModal(modalId) {
    var modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.remove('active');
        modal.classList.add('closing');

        // Wait exactly 300ms (matching the CSS transition time) before fully clearing states
        setTimeout(function () {
            modal.classList.remove('closing');
            document.body.style.overflow = '';
        }, 300);
    }
}


// ========== TOAST NOTIFICATION SLIDE SYSTEM ==========
function showToast(message, type) {
    // Clear out any old running toast instantly
    var existingToast = document.querySelector('.toast-notification');
    if (existingToast) { existingToast.remove(); }

    var toast = document.createElement('div');
    toast.className = 'toast-notification';

    if (type === 'error') {
        toast.classList.add('error');
        toast.innerHTML = '<i class="fas fa-exclamation-circle"></i> ' + message;
    } else if (type === 'warning') {
        toast.classList.add('warning');
        toast.innerHTML = '<i class="fas fa-exclamation-triangle"></i> ' + message;
    } else {
        toast.innerHTML = '<i class="fas fa-check-circle"></i> ' + message;
    }

    document.body.appendChild(toast);

    // Force animation frame loop update so slide begins smoothly from off-screen base layout
    void toast.offsetWidth;
    toast.classList.add('active');

    // Automatically slide back off-screen to the right after 3.5 seconds
    var hideTimeout = setTimeout(function () {
        if (toast) {
            toast.classList.remove('active');
            toast.classList.add('closing');

            // Wait for transition animation finish before clearing structural HTML nodes
            setTimeout(function () { if (toast) toast.remove(); }, 400);
        }
    }, 3500);

    // Allow user to click to dismiss toast instantly via exit transitions
    toast.onclick = function () {
        clearTimeout(hideTimeout);
        toast.classList.remove('active');
        toast.classList.add('closing');
        setTimeout(function () { if (toast) toast.remove(); }, 400);
    };
}

// ========== ACTIVE STATE TARGET SCALARS ==========
var currentDeactivateUserId = null;
var currentDeactivateFullName = null;
var currentDeactivateUsername = null;
var currentDeactivateRole = null;

var currentActivateUserId = null;
var currentActivateFullName = null;
var currentActivateUsername = null;
var currentActivateRole = null;

var currentDeleteUserId = null;
var currentDeleteFullName = null;
var currentDeleteUsername = null;
var currentDeleteRole = null;

var currentUnlockUserId = null;
var currentUnlockFullName = null;
var currentUnlockUsername = null;
var currentUnlockRole = null;

// ========== MODAL CALL HANDLERS ==========
function openDeactivateModal(userId, fullName, username, role) {
    currentDeactivateUserId = userId;
    currentDeactivateFullName = fullName;
    currentDeactivateUsername = username;
    currentDeactivateRole = role;
    openModal('deactivateModal', { fullName: fullName, username: username, role: role });
}

function closeDeactivateModal() {
    closeModal('deactivateModal');
    currentDeactivateUserId = null;
}

function openActivateModal(userId, fullName, username, role) {
    currentActivateUserId = userId;
    currentActivateFullName = fullName;
    currentActivateUsername = username;
    currentActivateRole = role;
    openModal('activateModal', { fullName: fullName, username: username, role: role });
}

function closeActivateModal() {
    closeModal('activateModal');
    currentActivateUserId = null;
}

// Fixed validation binding rules syntax errors
function openDeleteModal(userId, fullName, username, role) {
    currentDeleteUserId = userId;
    currentDeleteFullName = fullName;
    currentDeleteUsername = username;
    currentDeleteRole = role;
    openModal('deleteModal', { fullName: fullName, username: username, role: role });
}

function closeDeleteModal() {
    closeModal('deleteModal');
    currentDeleteUserId = null;
}

function openUnlockModal(userId, fullName, username, role) {
    currentUnlockUserId = userId;
    currentUnlockFullName = fullName;
    currentUnlockUsername = username;
    currentUnlockRole = role;

    document.getElementById('unlockFullName').textContent = fullName || 'N/A';
    document.getElementById('unlockUsername').textContent = username || 'N/A';
    document.getElementById('unlockRole').textContent = role || 'N/A';
    document.getElementById('unlockMessage').innerHTML = 'Are you sure you want to unlock user <strong>' + escapeHtml(fullName || username) + '</strong>?';

    document.getElementById('unlockModal').classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeUnlockModal() {
    var modal = document.getElementById('unlockModal');
    modal.classList.add('closing');
    setTimeout(function () {
        modal.classList.remove('active');
        modal.classList.remove('closing');
        document.body.style.overflow = '';
    }, 200);
    currentUnlockUserId = null;
}

// ========== TRANSACTION POST FETCH AJAX SUBMISSIONS ==========
function confirmDeactivate() {
    if (!currentDeactivateUserId) return;

    // Capture the target ID in a local block-scoped variable immediately
    var targetUserId = currentDeactivateUserId;
    var btn = document.getElementById('confirmDeactivateBtn');
    if (btn) btn.disabled = true;

    fetch('/Home/DeactivateUser', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'userId=' + encodeURIComponent(targetUserId)
    })
        .then(r => r.json())
        .then(data => {
            var isSuccess = data.success !== undefined ? data.success : data.Success;
            var responseMessage = data.message || data.Message || "Operation completed.";

            if (isSuccess) {
                showToast(responseMessage, 'success');
                closeDeactivateModal(); // Safe to close now because we preserved the ID locally

                // Pass the local, safe block variable context
                updateUserRow(targetUserId, 'deactivated');
            } else {
                showToast(responseMessage, 'error');
            }
        })
        .catch(err => showToast('Server communication error.', 'error'))
        .finally(() => { if (btn) btn.disabled = false; });
}

function confirmActivate() {
    if (!currentActivateUserId) return;

    var targetUserId = currentActivateUserId;
    var btn = document.getElementById('confirmActivateBtn');
    if (btn) btn.disabled = true;

    fetch('/Home/ActivateUser', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'userId=' + encodeURIComponent(targetUserId)
    })
        .then(r => r.json())
        .then(data => {
            var isSuccess = data.success !== undefined ? data.success : data.Success;
            var responseMessage = data.message || data.Message || "Operation completed.";

            if (isSuccess) {
                showToast(responseMessage, 'success');
                closeActivateModal();
                updateUserRow(targetUserId, 'activated');
            } else {
                showToast(responseMessage, 'error');
            }
        })
        .catch(err => showToast('Server communication error.', 'error'))
        .finally(() => { if (btn) btn.disabled = false; });
}

function confirmUnlock() {
    if (!currentUnlockUserId) return;

    var targetUserId = currentUnlockUserId;
    var btn = document.getElementById('confirmUnlockBtn');
    if (btn) btn.disabled = true;

    fetch('/Home/UnlockUser', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'userId=' + encodeURIComponent(targetUserId)
    })
        .then(r => r.json())
        .then(data => {
            var isSuccess = data.success !== undefined ? data.success : data.Success;
            var responseMessage = data.message || data.Message || "Operation completed.";

            if (isSuccess) {
                showToast(responseMessage, 'success');
                closeUnlockModal();
                updateUserRow(targetUserId, 'activated');
            } else {
                showToast(responseMessage, 'error');
            }
        })
        .catch(err => showToast('Server communication error.', 'error'))
        .finally(() => { if (btn) btn.disabled = false; });
}

function confirmDelete() {
    if (!currentDeleteUserId) return;

    var targetUserId = currentDeleteUserId;
    var btn = document.getElementById('confirmDeleteBtn');
    if (btn) btn.disabled = true;

    fetch('/Home/DeleteUser', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'userId=' + encodeURIComponent(targetUserId)
    })
        .then(r => r.json())
        .then(data => {
            var isSuccess = data.success !== undefined ? data.success : data.Success;
            var responseMessage = data.message || data.Message || "Operation completed.";

            if (isSuccess) {
                showToast(responseMessage, 'success');
                closeDeleteModal();
                removeUserRow(targetUserId);
            } else {
                showToast(responseMessage, 'error');
            }
        })
        .catch(err => showToast('Server communication error.', 'error'))
        .finally(() => { if (btn) btn.disabled = false; });
}

// ========== ROW UPDATE FUNCTIONS (FIXED TRACKING ATTRIBUTES) ==========
// ========== SURGICAL INLINE SINGLE-ROW MANIPULATION HANDLERS ==========
function updateUserRow(userId, action) {
    // Loose comparison loop over tr elements to avoid string/int attribute mismatches
    var rows = document.querySelectorAll('#userTableBody tr');
    var row = null;

    for (var i = 0; i < rows.length; i++) {
        var currentId = rows[i].getAttribute('data-userid');
        if (currentId && parseInt(currentId, 10) === parseInt(userId, 10)) {
            row = rows[i];
            break;
        }
    }

    if (!row) {
        console.error("UI Error: Could not locate row element for User ID:", userId);
        return;
    }

    // Safely pull text metrics directly from the matched DOM row columns
    var username = row.cells[1].textContent.trim();
    var fullName = row.cells[2].textContent.trim();
    var role = row.cells[3].textContent.trim();

    if (action === 'deactivated') {
        // Updated text to 'Deactivated' to match your new list styling blueprint
        row.cells[4].innerHTML = '<span class="badge badge-inactive">Deactivated</span>';
        row.cells[6].innerHTML = `
        <div style="display: flex; gap: 8px;">
            <button class="btn-icon btn-activate" onclick="openActivateModal(${userId}, '${escapeHtml(fullName)}', '${escapeHtml(username)}', '${escapeHtml(role)}')" title="Activate Account">
                <i class="fas fa-check-circle"></i>
            </button>
            <button class="btn-icon btn-delete" onclick="openDeleteModal(${userId}, '${escapeHtml(fullName)}', '${escapeHtml(username)}', '${escapeHtml(role)}')" title="Delete User">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    `;
        row.style.backgroundColor = '#fff3e0';


    } else if (action === 'activated') {
        // Injects the premium border-matched Active badge
        row.cells[4].innerHTML = '<span class="badge badge-active">Active</span>';
        row.cells[6].innerHTML = `
        <div style="display: flex; gap: 8px;">
            <button class="btn-icon btn-deactivate" onclick="openDeactivateModal(${userId}, '${escapeHtml(fullName)}', '${escapeHtml(username)}', '${escapeHtml(role)}')" title="Deactivate Account">
                <i class="fas fa-ban"></i>
            </button>
            <button class="btn-icon btn-delete" onclick="openDeleteModal(${userId}, '${escapeHtml(fullName)}', '${escapeHtml(username)}', '${escapeHtml(role)}')" title="Delete User">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    `;
        row.style.backgroundColor = '#e8f5e9';
    }

    // Smooth styling fade transition resetting baseline values cleanly
    row.style.transition = 'all 0.4s ease';
    setTimeout(function () {
        if (row) row.style.backgroundColor = '';
    }, 1000);
}

function removeUserRow(userId) {
    var rows = document.querySelectorAll('#userTableBody tr');
    var row = null;

    for (var i = 0; i < rows.length; i++) {
        var currentId = rows[i].getAttribute('data-userid');
        if (currentId && parseInt(currentId, 10) === parseInt(userId, 10)) {
            row = rows[i];
            break;
        }
    }

    if (!row) return;

    row.style.transition = 'all 0.3s ease';
    row.style.backgroundColor = '#ffebee';
    row.style.opacity = '0';
    row.style.transform = 'translateX(30px)';

    setTimeout(function () {
        if (row) row.remove();
        var tbody = document.getElementById('userTableBody');
        if (tbody && tbody.querySelectorAll('tr').length === 0) {
            tbody.innerHTML = '<tr class="empty-row"><td colspan="7"><div class="empty-state"><i class="fas fa-users"></i><p>No user accounts found.</p></div></td></tr>';
        }
    }, 300);
}

function updateUserRowAfterUnlock(userId, fullName, username, role) {
    var row = document.querySelector(`#userTableBody tr[data-userid="${userId}"]`);
    if (!row) return;

    var currentFullName = fullName || row.cells[2].textContent.trim();
    var currentUsername = username || row.cells[1].textContent.trim();
    var currentRole = role || row.cells[3].textContent.trim();

    // Inject matching active badge layout parameters
    row.cells[4].innerHTML = '<span class="badge badge-active">Active</span>';
    row.cells[6].innerHTML = `
        <div style="display: flex; gap: 8px;">
            <button class="btn-icon btn-deactivate" onclick="openDeactivateModal(${userId}, '${escapeHtml(currentFullName)}', '${escapeHtml(currentUsername)}', '${escapeHtml(currentRole)}')" title="Deactivate Account">
                <i class="fas fa-ban"></i>
            </button>
            <button class="btn-icon btn-delete" onclick="openDeleteModal(${userId}, '${escapeHtml(currentFullName)}', '${escapeHtml(currentUsername)}', '${escapeHtml(currentRole)}')" title="Delete User">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    `;
    row.style.transition = 'all 0.3s ease';
    row.style.backgroundColor = '#fff8e1';
    setTimeout(function () { if (row) row.style.backgroundColor = ''; }, 1000);
}

// ========== INITIALIZE ENTRY POINT ==========
document.addEventListener('DOMContentLoaded', function () {
    if (document.body.getAttribute('data-logged-in') === 'true') {
        startSessionChecker();
    }
});

// ========== EXPOSE GLOBAL PERMISSIONS MATRIX ==========
window.openDeactivateModal = openDeactivateModal;
window.openActivateModal = openActivateModal;
window.openDeleteModal = openDeleteModal;
window.openUnlockModal = openUnlockModal;

window.closeDeactivateModal = closeDeactivateModal;
window.closeActivateModal = closeActivateModal;
window.closeDeleteModal = closeDeleteModal;
window.closeUnlockModal = closeUnlockModal;

window.confirmDeactivate = confirmDeactivate;
window.confirmActivate = confirmActivate;
window.confirmDelete = confirmDelete;
window.confirmUnlock = confirmUnlock;


// ========== GLOBAL DATA LOOKUP POLLING ENGINE ==========
var refreshInterval;

// Pull state fragments from persistent browser tab cache storage memory
function getKnownLockedUsers() {
    var stored = sessionStorage.getItem('knownLockedUserIds');
    return stored ? JSON.parse(stored) : [];
}

function saveKnownLockedUsers(arr) {
    sessionStorage.setItem('knownLockedUserIds', JSON.stringify(arr));
}

function startAutoRefresh() {
    // If we happen to be on the User Management screen, synchronize baseline table entries
    var tbody = document.getElementById('userTableBody');
    var currentLockedList = getKnownLockedUsers();

    if (tbody) {
        document.querySelectorAll('#userTableBody tr[data-userid]').forEach(function (row) {
            var statusBadge = row.querySelector('td:nth-child(5) .badge');
            if (statusBadge && statusBadge.textContent.trim() === 'Locked') {
                var id = parseInt(row.getAttribute('data-userid'), 10);
                if (!currentLockedList.includes(id)) currentLockedList.push(id);
            }
        });
        saveKnownLockedUsers(currentLockedList);
    }

    // Run background fetch loop universally on EVERY page visit
    refreshInterval = setInterval(function () {
        globalRefreshTable();
    }, 5000);
}

function stopAutoRefresh() {
    if (refreshInterval) { clearInterval(refreshInterval); }
}

function globalRefreshTable() {
    fetch('/Home/GetUsers')
        .then(response => response.json())
        .then(data => {
            if (!data || !Array.isArray(data)) return;

            var knownLockedUserIds = getKnownLockedUsers();
            var tbody = document.getElementById('userTableBody'); // Check if table is present on current page view

            data.forEach(function (user) {
                var userId = user.userId !== undefined ? user.userId : user.User_ID;
                var isLocked = user.accountLocked !== undefined ? user.accountLocked : user.Account_Locked;
                var username = user.username || user.Username;
                var fullName = user.fullName || (user.Employee ? user.Employee.Full_Name : '') || (user.employee ? user.employee.full_Name : 'N/A');
                var role = user.role || user.Role;

                // CRITICAL VERIFICATION: Is this lockout fresh and unknown to this session?
                if (isLocked && !knownLockedUserIds.includes(userId)) {
                    knownLockedUserIds.push(userId);
                    saveKnownLockedUsers(knownLockedUserIds); // Save instantly to prevent duplicate popups across tabs

                    // Fire the smooth warning toast notification with clean stacked rows
                    var warningMessage = `
                    <div style="display: flex; flex-direction: column; gap: 4px; text-align: left; width: 100%;">
                        <span style="font-weight: 700; font-size: 14px;"><i class="fas fa-exclamation-triangle"></i> Security Alert: Account Locked</span>
                        <span style="font-size: 13px; opacity: 0.95;"><strong>Full Name:</strong> ${escapeHtml(fullName)}</span>
                        <span style="font-size: 13px; opacity: 0.95;"><strong>Username:</strong> ${escapeHtml(username)}</span>
                    </div>
                `;
                    showToast(warningMessage, 'warning');

                    // OPTIONAL STEP 3: If the admin happens to be looking at UserManagement.cshtml right now, update the row live!
                    if (tbody) {
                        var row = tbody.querySelector(`tr[data-userid="${userId}"]`);
                        if (row) {
                            row.cells[4].innerHTML = '<span class="badge badge-locked">Locked</span>';
                            row.cells[6].innerHTML = `
                            <div style="display: flex; gap: 8px;">
                                <button class="btn-icon btn-unlock" onclick="openUnlockModal(${userId}, '${escapeHtml(fullName)}', '${escapeHtml(username)}', '${escapeHtml(role)}')" title="Unlock Account">
                                    <i class="fas fa-lock-open"></i>
                                </button>
                                <button class="btn-icon btn-delete" onclick="openDeleteModal(${userId}, '${escapeHtml(fullName)}', '${escapeHtml(username)}', '${escapeHtml(role)}')" title="Delete User">
                                    <i class="fas fa-trash"></i>
                                </button>
                            </div>
                        `;
                            row.style.transition = 'all 0.4s ease';
                            row.style.backgroundColor = '#ffebee';
                            setTimeout(function () { if (row) row.style.backgroundColor = ''; }, 1500);
                        }
                    }
                }

                // If the user gets unlocked anywhere, clean them out of the tracking list
                if (!isLocked && knownLockedUserIds.includes(userId)) {
                    var index = knownLockedUserIds.indexOf(userId);
                    if (index > -1) {
                        knownLockedUserIds.splice(index, 1);
                        saveKnownLockedUsers(knownLockedUserIds);
                    }
                }
            });
        })
        .catch(error => console.error('Global background status poll error:', error));
}

// Start auto-refresh processing cycles globally
document.addEventListener('DOMContentLoaded', function () {
    startAutoRefresh();
});
window.addEventListener('beforeunload', function () {
    stopAutoRefresh();
});