namespace PartyClap.Models
{
    public class AdminVendorCalendarSummary
    {
        public string VendorId { get; set; }
        public string VendorName { get; set; }
        public string PinCode { get; set; }
        public string Phone { get; set; }
        public bool IsRegistered { get; set; }
        public int ServiceCount { get; set; }
        public int BookedDays { get; set; }
        public int UnderProcessDays { get; set; }
        public int AvailableDays { get; set; }
    }
}
