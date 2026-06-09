namespace PartyClap.Models
{
    public class VendorScheduleSlot
    {
        public int? BlockId { get; set; }
        public string SlotType { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Label { get; set; }
        public bool IsHourly { get; set; }
        public bool Editable { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string ServiceType { get; set; }
        public string EventType { get; set; }
        public string PartyLocation { get; set; }
        public string PartyPinCode { get; set; }
        public string Source { get; set; }
        public string Status { get; set; }
        public string RelatedId { get; set; }
        public decimal? TotalCost { get; set; }
        public int? GuestCount { get; set; }
    }
}
