using System;
using System.ComponentModel.DataAnnotations;

namespace PartyClap.Models
{
    public class Address
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }

        [Required]
        public string Label { get; set; } // Home, Office, etc.

        [Required]
        public string RecipientName { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        public string Phone { get; set; }

        [Required]
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Enter a valid 6-digit PIN code.")]
        public string PinCode { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
