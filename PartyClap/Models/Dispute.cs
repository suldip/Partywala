using System;

namespace PartyClap.Models
{
    public class Dispute
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string BookingId { get; set; }
        public string RaisedByUserId { get; set; } // Customer or Vendor ID
        public string UserRole { get; set; } // Customer/Vendor
        public string Reason { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "Open"; // Open, Under Investigation, Resolved, Legal Escalation
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        
        // Navigation properties for UI
        public string BookingDisplayId { get; set; }
        public string OpposingPartyName { get; set; }
    }
}
