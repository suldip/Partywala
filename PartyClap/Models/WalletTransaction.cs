using System;

namespace PartyClap.Models
{
    public class WalletTransaction
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string TransactionType { get; set; } // Credit, Debit, Refund
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public string BookingId { get; set; }
    }
}
