using System;

namespace PartyClap.Models
{
    public class VendorCalendarBlock
    {
        public int Id { get; set; }
        public string VendorId { get; set; }
        public DateTime BlockDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsHourly { get; set; }
        public string Label { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
