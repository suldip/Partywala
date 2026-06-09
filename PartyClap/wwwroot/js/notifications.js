// Notification System
let notificationCheckInterval;

function escapeHtml(text) {
    if (text == null) return '';
    return String(text)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

function pickNotificationField(n, camel, pascal) {
    if (!n) return '';
    var val = n[camel];
    if (val !== undefined && val !== null && val !== '') return val;
    return n[pascal];
}

function loadNotifications() {
    fetch((window.basePath || '') + 'api/notifications/unread', { credentials: 'same-origin' })
        .then(function (response) { return response.json(); })
        .then(function (data) {
            var count = data.count != null ? data.count : (data.Count || 0);
            var list = data.notifications || data.Notifications || [];
            updateNotificationBadge(count);
            renderNotifications(list);
        })
        .catch(function (error) { console.error('Error loading notifications:', error); });
}

function updateNotificationBadge(count) {
    ['notification-badge', 'mobile-notification-badge'].forEach(function (id) {
        var badge = document.getElementById(id);
        if (!badge) return;
        if (count > 0) {
            badge.textContent = count > 9 ? '9+' : count;
            badge.style.display = 'inline';
        } else {
            badge.style.display = 'none';
        }
    });
}

function buildNotificationHtml(notifications) {
    if (!notifications || notifications.length === 0) {
        return '<div class="dropdown-item text-center text-muted py-4">No new notifications</div>';
    }

    return notifications.map(function (n) {
        var id = pickNotificationField(n, 'id', 'Id') || '';
        var title = escapeHtml(pickNotificationField(n, 'title', 'Title') || 'Notification');
        var message = escapeHtml(pickNotificationField(n, 'message', 'Message') || '');
        var icon = pickNotificationField(n, 'icon', 'Icon') || '🔔';
        var timeAgo = escapeHtml(pickNotificationField(n, 'timeAgo', 'TimeAgo') || 'Just now');
        var actionUrl = pickNotificationField(n, 'actionUrl', 'ActionUrl');
        var href = '#';
        if (actionUrl) {
            if (actionUrl.indexOf('http') === 0 || actionUrl.charAt(0) === '/') {
                href = actionUrl;
            } else {
                href = (window.basePath || '') + actionUrl;
            }
        }

        return (
            '<a class="dropdown-item notification-item unread" href="' + escapeHtml(href) + '" ' +
            'onclick="markAsRead(\'' + escapeHtml(id) + '\'); return true;">' +
            '<div class="d-flex align-items-start gap-2">' +
            '<div class="notification-icon">' + icon + '</div>' +
            '<div class="flex-grow-1 min-w-0">' +
            '<div class="fw-bold text-truncate">' + title + '</div>' +
            '<div class="small text-muted notification-message">' + message + '</div>' +
            '<div class="small text-muted mt-1"><i class="bi bi-clock"></i> ' + timeAgo + '</div>' +
            '</div>' +
            '<div class="unread-dot align-self-center"></div>' +
            '</div>' +
            '</a>'
        );
    }).join('');
}

function renderNotifications(notifications) {
    var html = buildNotificationHtml(notifications);
    ['notification-list', 'mobile-notification-list'].forEach(function (id) {
        var list = document.getElementById(id);
        if (list) list.innerHTML = html;
    });
}

function markAsRead(notificationId) {
    if (!notificationId) return;
    fetch((window.basePath || '') + 'api/notifications/' + encodeURIComponent(notificationId) + '/read', { method: 'POST', credentials: 'same-origin' })
        .then(function () { loadNotifications(); })
        .catch(function (error) { console.error('Error marking notification as read:', error); });
}

function markAllAsRead() {
    fetch((window.basePath || '') + 'api/notifications/mark-all-read', { method: 'POST', credentials: 'same-origin' })
        .then(function () { loadNotifications(); })
        .catch(function (error) { console.error('Error marking all as read:', error); });
    return false;
}

document.addEventListener('DOMContentLoaded', function () {
    if (document.getElementById('notification-badge') || document.getElementById('mobile-notification-badge')) {
        loadNotifications();
        notificationCheckInterval = setInterval(loadNotifications, 30000);
    }
});
