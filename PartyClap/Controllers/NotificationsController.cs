using Microsoft.AspNetCore.Mvc;
using PartyClap.Services;
using System.Linq;

namespace PartyClap.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("unread")]
        public IActionResult GetUnreadNotifications()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userType))
            {
                return Ok(new { notifications = new object[0], count = 0 });
            }

            var notifications = _notificationService.GetUserNotifications(userId, userType)
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Icon,
                    n.ActionUrl,
                    n.CreatedAt,
                    TimeAgo = GetTimeAgo(n.CreatedAt)
                })
                .ToList();

            var count = _notificationService.GetUnreadCount(userId, userType);

            return Ok(new { notifications, count });
        }

        [HttpPost("{id}/read")]
        public IActionResult MarkAsRead(string id)
        {
            _notificationService.MarkAsRead(id);
            return Ok();
        }

        [HttpPost("mark-all-read")]
        public IActionResult MarkAllAsRead()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userType))
            {
                _notificationService.MarkAllAsRead(userId, userType);
            }

            return Ok();
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            
            return dateTime.ToString("MMM dd");
        }
    }
}
