using System;
using System.ComponentModel.DataAnnotations;

namespace PartyClap.Models
{
    public class Customer
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } // Hashed before persistence
        public decimal WalletBalance { get; set; } = 0.00m;
        
        // Full Profile Fields
        public string ProfilePicture { get; set; }
        public string SecondaryPhone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public DateTime JoinedDate { get; set; } = DateTime.Now;
    }
}
