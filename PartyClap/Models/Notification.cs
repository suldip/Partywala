using System;

namespace PartyClap.Models
{
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } // Customer or Vendor ID
        public string UserType { get; set; } // "Customer" or "Vendor"
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // "BookingApproved", "BookingRejected", "PaymentReceived", etc.
        public string RelatedId { get; set; } // Booking ID, Payment ID, etc.
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public string Icon { get; set; } // Emoji or icon class
        public string ActionUrl { get; set; } // Where to redirect when clicked
    }
}
