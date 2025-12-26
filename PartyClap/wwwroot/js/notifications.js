// Notification System
let notificationCheckInterval;

function loadNotifications() {
    fetch((window.basePath || '') + 'api/notifications/unread')
        .then(response => response.json())
        .then(data => {
            updateNotificationBadge(data.count);
            renderNotifications(data.notifications);
        })
        .catch(error => console.error('Error loading notifications:', error));
}

function updateNotificationBadge(count) {
    const badge = document.getElementById('notification-badge');
    if (badge) {
        if (count > 0) {
            badge.textContent = count > 9 ? '9+' : count;
            badge.style.display = 'inline';
        } else {
            badge.style.display = 'none';
        }
    }
}

function renderNotifications(notifications) {
    const list = document.getElementById('notification-list');
    if (!list) return;

    if (notifications.length === 0) {
        list.innerHTML = '<div class="dropdown-item text-center text-muted">No new notifications</div>';
        return;
    }

    list.innerHTML = notifications.map(n => `
        <a class="dropdown-item notification-item unread" 
           href="${n.actionUrl}" 
           onclick="markAsRead('${n.id}'); return true;">
            <div class="d-flex align-items-start">
                <div class="notification-icon me-3">
                    ${n.icon}
                </div>
                <div class="flex-grow-1">
                    <div class="fw-bold">${n.title}</div>
                    <div class="small text-muted">${n.message}</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-clock"></i> ${n.timeAgo}
                    </div>
                </div>
                <div class="unread-dot"></div>
            </div>
        </a>
    `).join('');
}

function markAsRead(notificationId) {
    fetch(`${window.basePath || ''}api/notifications/${notificationId}/read`, { method: 'POST' })
        .then(() => loadNotifications())
        .catch(error => console.error('Error marking notification as read:', error));
}

function markAllAsRead() {
    fetch((window.basePath || '') + 'api/notifications/mark-all-read', { method: 'POST' })
        .then(() => loadNotifications())
        .catch(error => console.error('Error marking all as read:', error));
    return false; // Prevent default link behavior
}

// Initialize notifications when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    // Only load notifications if user is logged in (check if notification bell exists)
    if (document.getElementById('notification-badge')) {
        loadNotifications();
        // Poll for new notifications every 30 seconds
        notificationCheckInterval = setInterval(loadNotifications, 30000);
    }
});
