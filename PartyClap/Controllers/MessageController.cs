using Microsoft.AspNetCore.Mvc;
using PartyClap.Models;
using PartyClap.Services;

namespace PartyClap.Controllers
{
    public class MessageController : Controller
    {
        private readonly IDataService _dataService;

        public MessageController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpPost]
        public IActionResult SendInitialMessage(string vendorId, string content)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to send messages." });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Message content cannot be empty." });
            }

            try
            {
                var message = new Message
                {
                    SenderId = userId,
                    ReceiverId = vendorId,
                    SenderRole = userRole,
                    Content = content,
                    Timestamp = DateTime.Now
                };

                _dataService.SendMessage(message);

                return Json(new { success = true, message = "Message sent successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error sending message: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetChatHistory(string otherUserId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            try
            {
                var history = _dataService.GetChatHistory(userId, otherUserId);
                return Json(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
