using System.Collections.Generic;

namespace PartyClap.Models
{
    public class PaymentCheckoutViewModel
    {
        public List<CartRequestItem> Items { get; set; } = new List<CartRequestItem>();
        public decimal Subtotal { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public bool IsSuccess { get; set; }
        public decimal PaidAmount { get; set; }
        public string SuccessMessage { get; set; }
    }
}
