using PartyClap.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PartyClap.Services
{
    public class NotificationService : INotificationService
    {
        private static List<Notification> _notifications = new List<Notification>();

        public void CreateNotification(Notification notification)
        {
            _notifications.Add(notification);
        }

        public List<Notification> GetUserNotifications(string userId, string userType)
        {
            return _notifications
                .Where(n => n.UserId == userId && n.UserType == userType)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public int GetUnreadCount(string userId, string userType)
        {
            return _notifications
                .Count(n => n.UserId == userId && n.UserType == userType && !n.IsRead);
        }

        public void MarkAsRead(string notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
            }
        }

        public void MarkAllAsRead(string userId, string userType)
        {
            var userNotifications = _notifications
                .Where(n => n.UserId == userId && n.UserType == userType);
            
            foreach (var notification in userNotifications)
            {
                notification.IsRead = true;
            }
        }

        public Notification GetNotification(string notificationId)
        {
            return _notifications.FirstOrDefault(n => n.Id == notificationId);
        }
    }
}
