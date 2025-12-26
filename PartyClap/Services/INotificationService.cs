using PartyClap.Models;
using System.Collections.Generic;

namespace PartyClap.Services
{
    public interface INotificationService
    {
        void CreateNotification(Notification notification);
        List<Notification> GetUserNotifications(string userId, string userType);
        int GetUnreadCount(string userId, string userType);
        void MarkAsRead(string notificationId);
        void MarkAllAsRead(string userId, string userType);
        Notification GetNotification(string notificationId);
    }
}
