using System;

namespace PartyClap.Models
{
    public class Booking
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CustomerId { get; set; }
        public string VendorId { get; set; }
        public string ServiceId { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.Now;
        public DateTime EventDate { get; set; }
        
        public decimal VendorCost { get; set; }
        public decimal CustomerTotalCost { get; set; } // VendorCost + 10%
        public decimal AdvancePaid { get; set; } // 20% of VendorCost
        public decimal BalanceAmount { get; set; } // CustomerTotalCost - AdvancePaid
        
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed
        public bool BalancePaidOnApp { get; set; } = false;
    }
}
