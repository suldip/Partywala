using System;
using System.Collections.Generic;

namespace PartyClap.Models
{
    public class Vendor
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string PinCode { get; set; }
        public string Password { get; set; } // Insecure for MVP, but functional
        public bool IsRegistered { get; set; } = false; // True after paying 500
        public int TrustScore { get; set; } = 100;
        public List<ServiceListing> Services { get; set; } = new List<ServiceListing>();
        public List<string> ServiceLocations { get; set; } = new List<string>(); // List of PinCodes
        public decimal WalletBalance { get; set; } = 0;
        
        // Bank Details
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string IfscCode { get; set; }
        public string UpiId { get; set; }
    }
}
