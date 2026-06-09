namespace PartyClap.Models
{
    public class ReviewSummary
    {
        public string ServiceId { get; set; }
        public string VendorId { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
