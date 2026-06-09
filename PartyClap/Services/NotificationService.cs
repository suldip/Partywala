using PartyClap.DAL;
using PartyClap.Models;
using System.Collections.Generic;
using System.Linq;

namespace PartyClap.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationDAL _notificationDAL;

        public NotificationService(NotificationDAL notificationDAL)
        {
            _notificationDAL = notificationDAL;
        }

        public void CreateNotification(Notification notification)
        {
            _notificationDAL.CreateNotification(notification);
        }

        public List<Notification> GetUserNotifications(string userId, string userType)
        {
            return _notificationDAL.GetUserNotifications(userId, userType)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public int GetUnreadCount(string userId, string userType)
        {
            return _notificationDAL.GetUnreadCount(userId, userType);
        }

        public void MarkAsRead(string notificationId)
        {
            _notificationDAL.MarkAsRead(notificationId);
        }

        public void MarkAllAsRead(string userId, string userType)
        {
            _notificationDAL.MarkAllAsRead(userId, userType);
        }

        public Notification GetNotification(string notificationId)
        {
            return _notificationDAL.GetNotification(notificationId);
        }
    }
}
