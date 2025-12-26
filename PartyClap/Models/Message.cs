using System;

namespace PartyClap.Models
{
    public class Message
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string SenderRole { get; set; } // Customer or Vendor
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        
        // Navigation properties for UI
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }
    }
}
