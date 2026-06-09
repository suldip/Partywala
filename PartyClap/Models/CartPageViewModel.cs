using System.Collections.Generic;

namespace PartyClap.Models
{
    public class CartRequestItem
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string VendorName { get; set; }
        public string ServiceType { get; set; }
        public System.DateTime EventDate { get; set; }
        public System.DateTime? EventEndDate { get; set; }
        public string EventStartTime { get; set; }
        public string EventEndTime { get; set; }
        public int DayCount { get; set; } = 1;
        public decimal TotalCost { get; set; }
        public string Status { get; set; }
        public string PartyLocation { get; set; }
        public string PartyPinCode { get; set; }
        public string PaymentUrl { get; set; }
        public decimal GstAmount { get; set; }
        public decimal GrandTotal { get; set; }
    }

    public class CartPageViewModel
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public List<CartRequestItem> PendingRequests { get; set; } = new List<CartRequestItem>();
        public List<CartRequestItem> AcceptedRequests { get; set; } = new List<CartRequestItem>();
        public decimal AcceptedSubtotal { get; set; }
        public decimal AcceptedGstAmount { get; set; }
        public decimal AcceptedGrandTotal { get; set; }
        public decimal GstPercent { get; set; }
        public List<string> ExpiredVendorNotices { get; set; } = new List<string>();

        public bool HasContent =>
            Items.Count > 0 || PendingRequests.Count > 0 || AcceptedRequests.Count > 0
            || ExpiredVendorNotices.Count > 0;
    }
}
