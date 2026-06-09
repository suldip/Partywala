using System;
using System.ComponentModel.DataAnnotations;

namespace PartyClap.Models
{
    public class ServiceListing
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string VendorId { get; set; }
        public string ServiceType { get; set; } // e.g., Singer, Magician
        public string Description { get; set; }

        [Range(typeof(decimal), "1", "79228162514264337593543950335", ErrorMessage = "Price must be at least ₹1.")]
        public decimal Cost { get; set; }
        public string Unit { get; set; } // Hour, Event, Person
        public string MediaUrl { get; set; }
        public string Attributes { get; set; } // JSON string for extra details
        public decimal? WeekendCost { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string VendorName { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string PinCode { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Address { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Microsoft.AspNetCore.Http.IFormFile MediaFile { get; set; }
    }
}
