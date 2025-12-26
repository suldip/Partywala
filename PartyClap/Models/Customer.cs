using System;

namespace PartyClap.Models
{
    public class Customer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; } // Not stored directly, hashed in real app
        public decimal WalletBalance { get; set; } = 0.00m;
    }
}
