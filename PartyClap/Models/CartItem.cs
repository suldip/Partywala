using System;

namespace PartyClap.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string CookieId { get; set; }
        public string ServiceId { get; set; }
        public string VendorId { get; set; }
        public string ServiceType { get; set; }
        public string VendorName { get; set; }
        public decimal Cost { get; set; }
        public decimal? WeekendCost { get; set; }
        public DateTime? EventDate { get; set; }
        public string Unit { get; set; }
        public string MediaUrl { get; set; }
    }
}
