using System;

namespace PartyClap.Models
{
    public class Review
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string BookingId { get; set; }
        public string CustomerId { get; set; }
        public string VendorId { get; set; }
        public string ServiceId { get; set; }
        
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties (not used in database but helpful for views)
        public string CustomerName { get; set; }
        public string ServiceName { get; set; }
    }
}
