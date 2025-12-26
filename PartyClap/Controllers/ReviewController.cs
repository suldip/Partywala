using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PartyClap.Models;
using PartyClap.Services;

namespace PartyClap.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IDataService _dataService;

        public ReviewController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpPost]
        public IActionResult SubmitReview(string bookingId, string vendorId, string serviceId, int rating, string comment)
        {
            var customerId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(customerId))
            {
                return Json(new { success = false, message = "Please login to submit feedback." });
            }

            try
            {
                var review = new Review
                {
                    BookingId = bookingId,
                    CustomerId = customerId,
                    VendorId = vendorId,
                    ServiceId = serviceId,
                    Rating = rating,
                    Comment = comment
                };

                _dataService.AddReview(review);
                
                // Also update the booking status if needed, or just return success
                // In a real app, we might want to mark the booking as 'Reviewed' to prevent duplicates
                
                return Json(new { success = true, message = "Thank you for your feedback!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error submitting review: " + ex.Message });
            }
        }
    }
}
