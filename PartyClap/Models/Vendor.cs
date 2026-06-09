using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PartyClap.Models
{
    public class Vendor
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 150 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(254)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(14, MinimumLength = 10, ErrorMessage = "Enter a valid phone number.")]
        public string Phone { get; set; }
        public string ProfilePicture { get; set; }

        [Required(ErrorMessage = "Street address is required.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Street address must be between 5 and 500 characters.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Pin code is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Enter a valid 6-digit pin code.")]
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
