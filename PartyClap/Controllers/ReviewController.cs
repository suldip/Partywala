using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyClap.Models;
using PartyClap.Services;
using System;

namespace PartyClap.Controllers
{
    [Authorize(Roles = "Customer")]
    public class ReviewController : Controller
    {
        private readonly IDataService _dataService;

        public ReviewController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReview(string bookingId, string vendorId, string serviceId, int rating, string comment)
        {
            var customerId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(customerId))
            {
                return Json(new { success = false, message = "Please login to submit feedback." });
            }

            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Rating must be between 1 and 5." });
            }

            if (string.IsNullOrWhiteSpace(bookingId) || string.IsNullOrWhiteSpace(vendorId) || string.IsNullOrWhiteSpace(serviceId))
            {
                return Json(new { success = false, message = "Invalid review request." });
            }

            var booking = _dataService.GetBooking(bookingId);
            if (booking == null || booking.CustomerId != customerId)
            {
                return Json(new { success = false, message = "Booking not found or access denied." });
            }

            if (!string.Equals(booking.VendorId, vendorId, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(booking.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Review details do not match this booking." });
            }

            if (booking.Status != "Confirmed")
            {
                return Json(new { success = false, message = "You can review vendors after your booking is confirmed." });
            }

            if (booking.EventDate.Date > DateTime.Today)
            {
                return Json(new { success = false, message = "Please review after your event date has passed." });
            }

            if (_dataService.HasReviewForBooking(bookingId))
            {
                return Json(new { success = false, message = "You have already reviewed this booking." });
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
                    Comment = comment?.Trim(),
                    CreatedAt = DateTime.Now
                };

                _dataService.AddReview(review);
                return Json(new { success = true, message = "Thank you for your feedback!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error submitting review. Please try again." });
            }
        }
    }
}
