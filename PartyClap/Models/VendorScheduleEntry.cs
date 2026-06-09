using System;
using System.Collections.Generic;

namespace PartyClap.Models
{
    public class VendorScheduleEntry
    {
        public List<VendorScheduleSlot> Slots { get; set; } = new List<VendorScheduleSlot>();

        public DateTime Date { get; set; }

        public bool IsBooked { get; set; }

        public bool IsUnderProcess { get; set; }

        public string Source { get; set; }

        public string Status { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerEmail { get; set; }

        public string EventType { get; set; }

        public string ServiceType { get; set; }

        public string Label { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public string PartyLocation { get; set; }

        public string PartyPinCode { get; set; }

        public string EventRangeStart { get; set; }

        public string EventRangeEnd { get; set; }

        public int? DayCount { get; set; }

        public int? GuestCount { get; set; }

        public decimal? TotalCost { get; set; }

        public string RelatedId { get; set; }

    }

}


