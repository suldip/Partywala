using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyClap.Models;
using PartyClap.Services;

namespace PartyClap.Controllers
{
    [Authorize(Roles = "Customer,Vendor")]
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
            return SendMessageInternal(vendorId, content);
        }

        [HttpPost]
        public IActionResult SendMessage(string receiverId, string content)
        {
            return SendMessageInternal(receiverId, content);
        }

        [HttpGet]
        public IActionResult GetChatHistory(string otherUserId)
        {
            var userId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            if (string.IsNullOrWhiteSpace(otherUserId))
            {
                return Json(new { success = false, message = "Conversation partner is required." });
            }

            try
            {
                var history = _dataService.GetChatHistory(userId, otherUserId);
                return Json(new { success = true, data = history });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Could not load chat history." });
            }
        }

        private IActionResult SendMessageInternal(string receiverId, string content)
        {
            var userId = HttpContext.GetUserId();
            var userRole = HttpContext.GetUserRole();

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to send messages." });
            }

            if (string.IsNullOrWhiteSpace(receiverId))
            {
                return Json(new { success = false, message = "Recipient is required." });
            }

            content = content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Message content cannot be empty." });
            }

            if (content.Length > 2000)
            {
                return Json(new { success = false, message = "Message must be 2000 characters or fewer." });
            }

            if (!IsAllowedRecipient(userRole, receiverId))
            {
                return Json(new { success = false, message = "You cannot send a message to this recipient." });
            }

            try
            {
                var message = new Message
                {
                    SenderId = userId,
                    ReceiverId = receiverId,
                    SenderRole = userRole,
                    Content = content,
                    Timestamp = DateTime.Now
                };

                _dataService.SendMessage(message);

                var redirectUrl = BuildInboxUrl(userRole, receiverId);
                return Json(new
                {
                    success = true,
                    message = "Message sent successfully!",
                    redirectUrl
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error sending message. Please try again." });
            }
        }

        private bool IsAllowedRecipient(string senderRole, string receiverId)
        {
            if (string.Equals(senderRole, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                return _dataService.GetVendor(receiverId) != null;
            }

            if (string.Equals(senderRole, "Vendor", StringComparison.OrdinalIgnoreCase))
            {
                return _dataService.GetCustomerById(receiverId) != null;
            }

            return false;
        }

        private string BuildInboxUrl(string userRole, string otherUserId)
        {
            if (string.Equals(userRole, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                return Url.Action("Dashboard", "Customer", new { section = "Messages", chatWith = otherUserId }) ?? string.Empty;
            }

            return Url.Action("Dashboard", "Vendor", new { section = "messages", chatWith = otherUserId }) ?? string.Empty;
        }
    }
}
