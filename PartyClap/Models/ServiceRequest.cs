using System;

namespace PartyClap.Models
{
    public class ServiceRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CustomerId { get; set; }
        public string VendorId { get; set; }
        public string ServiceId { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime? EventEndDate { get; set; }
        public string EventStartTime { get; set; }
        public string EventEndTime { get; set; }
        public int DayCount { get; set; } = 1;
        public decimal TotalCost { get; set; }
        public string EventType { get; set; }
        public int GuestCount { get; set; }
        public string AdditionalDetails { get; set; }
        public string PartyLocation { get; set; }
        public string PartyPinCode { get; set; }
        public decimal? PartyLatitude { get; set; }
        public decimal? PartyLongitude { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Completed
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ResponseDate { get; set; }
    }
}

