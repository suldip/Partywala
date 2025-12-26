using Microsoft.AspNetCore.Http;

namespace PartyClap.Models
{
    public class PortfolioItem
    {
        public string Id { get; set; }
        public string VendorId { get; set; }
        public string MediaType { get; set; } // "Image" or "Audio"
        public string MediaUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public IFormFile File { get; set; }
    }
}
