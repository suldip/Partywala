using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PartyClap.Models;
using PartyClap.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PartyClap.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IDataService _dataService;

        public CustomerController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Customer customer)
        {
            if (ModelState.IsValid)
            {
                customer.Id = Guid.NewGuid().ToString();
                // In a real app, hash the password here
                _dataService.RegisterCustomer(customer);
                return RedirectToAction("Explore");
            }
            return View(customer);
        }
        
        public IActionResult Dashboard()
        {
            var customerId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");
            
            var customer = _dataService.GetCustomerById(customerId);
            var bookings = _dataService.GetCustomerBookings(customerId);
            var serviceRequests = _dataService.GetCustomerServiceRequestsWithDetails(customerId);
            var walletTransactions = _dataService.GetWalletTransactions(customerId, 5);
            
            // Calculate PartyPoints (Loyalty Program)
            int partyPoints = 0;
            decimal totalSpent = 0;
            
            if (bookings != null)
            {
                foreach (var b in bookings)
                {
                    if (b.Status == "Confirmed" || b.BalancePaidOnApp == true)
                    {
                        totalSpent += b.CustomerTotalCost;
                        partyPoints += 50; // 50 points per booking
                    }
                }
                // 1 point for every ₹100 spent
                partyPoints += (int)(totalSpent / 100);
            }
            
            // Determine Tier
            string tier = "Bronze";
            string nextTier = "Silver";
            int pointsForNext = 500;
            
            if (partyPoints >= 2000)
            {
                tier = "Gold";
                nextTier = "Platinum";
                pointsForNext = 5000;
            }
            else if (partyPoints >= 500)
            {
                tier = "Silver";
                nextTier = "Gold";
                pointsForNext = 2000;
            }

            ViewBag.Customer = customer;
            ViewBag.ServiceRequests = serviceRequests;
            ViewBag.WalletTransactions = walletTransactions;
            ViewBag.PartyPoints = partyPoints;
            ViewBag.LoyaltyTier = tier;
            ViewBag.NextTierPoints = pointsForNext;
            
            return View(bookings);
        }

        public IActionResult Explore(string search, string pinCode, decimal? minPrice, decimal? maxPrice, int? minRating, DateTime? eventDate)
        {
            var services = _dataService.SearchServices(search, pinCode, minPrice, maxPrice, minRating, eventDate); 
            
            // Preserve filters
            ViewBag.Search = search;
            ViewBag.PinCode = pinCode;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.MinRating = minRating;
            ViewBag.EventDate = eventDate?.ToString("yyyy-MM-dd");
            
            // Populate PinCodes for dropdown
            ViewBag.Locations = _dataService.GetLocations();
            
            return View(services);
        }

        public IActionResult Details(string serviceId)
        {
            // We need to fetch the service details. 
            // Since we don't have a direct GetService method in IDataService yet, 
            // we can iterate through vendors to find it (inefficient) or add GetService.
            // For MVP, let's add GetService to IDataService/DAL or cheat by fetching all.
            
            // Better approach: Add GetService to DAL.
            // For now, I'll implement a quick lookup in DAL or just mock it if time is tight.
            // Let's assume I add GetServiceById to VendorDAL and expose it.
            
            // Quick fix: Fetch all services from all vendors (very bad for prod, ok for MVP demo)
            // Or better: Pass the details via TempData? No, bad practice.
            
            // Let's add GetServiceById to VendorDAL properly.
            var service = _dataService.GetService(serviceId);
            // The following lines seem to be part of an incomplete booking creation.
            // For now, let's assume 'service' is what we need for the view.
            // If a booking was intended here, it needs to be properly structured.
            // For the purpose of this edit, we'll assume the Details action
            // should just return the service details.
            return View(service);
        }
        
        [HttpPost]
        public IActionResult RequestService([FromBody] ServiceRequestRequest request)
        {
            try
            {
                var customerId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(customerId))
                {
                    return Json(new { success = false, message = "Please login to request a service" });
                }

                // Get service to get vendor ID
                var service = _dataService.GetService(request.ServiceId);
                if (service == null)
                {
                    return Json(new { success = false, message = "Service not found" });
                }

                var serviceRequest = new ServiceRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerId,
                    VendorId = service.VendorId,
                    ServiceId = request.ServiceId,
                    EventDate = request.EventDate,
                    EventType = request.EventType,
                    GuestCount = request.GuestCount,
                    AdditionalDetails = request.AdditionalDetails,
                    Status = "Pending",
                    CreatedDate = DateTime.Now
                };

                _dataService.CreateServiceRequest(serviceRequest);

                return Json(new { success = true, message = "Service request submitted successfully! The vendor will contact you soon." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error submitting request: " + ex.Message });
            }
        }

        public class ServiceRequestRequest
        {
            public string ServiceId { get; set; }
            public string VendorId { get; set; }
            public DateTime EventDate { get; set; }
            public string EventType { get; set; }
            public int GuestCount { get; set; }
            public string AdditionalDetails { get; set; }
        }

        [HttpPost]
        public IActionResult AddToCart(string serviceId, string vendorId, DateTime? eventDate)
        {
            var cookieId = GetOrCreateCookieId();
            _dataService.AddToCart(cookieId, serviceId, vendorId, eventDate);
            return RedirectToAction("ViewCart");
        }

        [HttpPost]
        public IActionResult AddToCartJson(string serviceId, string vendorId, DateTime? eventDate)
        {
            try
            {
                var cookieId = GetOrCreateCookieId();
                _dataService.AddToCart(cookieId, serviceId, vendorId, eventDate);
                var items = _dataService.GetCartItems(cookieId);
                return Json(new { success = true, count = items.Count, message = "Added to cart" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetCartJson()
        {
            var cookieId = GetOrCreateCookieId();
            var items = _dataService.GetCartItems(cookieId);
            return Json(new { success = true, items = items, count = items.Count });
        }

        public IActionResult ViewCart()
        {
            var cookieId = GetOrCreateCookieId();
            var items = _dataService.GetCartItems(cookieId);
            return View("Cart", items);
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int cartItemId)
        {
            _dataService.RemoveFromCart(cartItemId);
            return RedirectToAction("ViewCart");
        }

        [HttpPost]
        public IActionResult UpdateCartItemDate([FromBody] UpdateCartItemDateRequest request)
        {
            try
            {
                var cookieId = GetOrCreateCookieId();
                _dataService.UpdateCartItemDate(request.CartItemId, request.EventDate);
                
                // Check if we need to recalculate (weekend pricing)
                var items = _dataService.GetCartItems(cookieId);
                var item = items.FirstOrDefault(i => i.Id == request.CartItemId);
                bool recalculate = false;
                
                if (item != null && request.EventDate.HasValue && item.WeekendCost.HasValue)
                {
                    var dayOfWeek = request.EventDate.Value.DayOfWeek;
                    if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                    {
                        recalculate = true; // Weekend pricing applies
                    }
                }
                
                return Json(new { success = true, recalculate = recalculate });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        public class UpdateCartItemDateRequest
        {
            public int CartItemId { get; set; }
            public DateTime? EventDate { get; set; }
        }

        [HttpPost]
        public IActionResult Checkout()
        {
            var cookieId = GetOrCreateCookieId();
            var items = _dataService.GetCartItems(cookieId);
            
            if (items.Count == 0) return RedirectToAction("ViewCart");

            var bookings = new List<Booking>();
            
            foreach (var item in items)
            {
                // Determine which cost to use based on EventDate
                decimal baseCost = item.Cost;
                if (item.EventDate.HasValue && item.WeekendCost.HasValue)
                {
                    var dayOfWeek = item.EventDate.Value.DayOfWeek;
                    if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                    {
                        baseCost = item.WeekendCost.Value;
                    }
                }
                
                decimal customerTotal = baseCost * 1.10m; // 10% markup
                decimal advance = baseCost * 0.20m; // 20% of vendor cost
                decimal balance = customerTotal - advance;
                
                var booking = new Booking
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = HttpContext.Session.GetString("UserId") ?? "c1", // Use session or mock
                    VendorId = item.VendorId,
                    ServiceId = item.ServiceId,
                    VendorCost = baseCost,
                    CustomerTotalCost = customerTotal,
                    AdvancePaid = advance,
                    BalanceAmount = balance,
                    EventDate = item.EventDate ?? DateTime.Now.AddDays(7), // Use cart EventDate or default
                    Status = "Requested", // Changed from "Confirmed" to "Requested"
                    BalancePaidOnApp = false
                };
                
                _dataService.AddBooking(booking);
                bookings.Add(booking);
            }
            
            _dataService.ClearCart(cookieId);
            
            return View("BookingConfirmation", bookings); 
        }
        
        public IActionResult VendorProfile(string vendorId)
        {
            var vendor = _dataService.GetVendor(vendorId);
            if (vendor == null) return NotFound();
            
            var portfolio = _dataService.GetVendorPortfolio(vendorId);
            var services = _dataService.GetVendorServices(vendorId);
            
            ViewBag.Portfolio = portfolio;
            ViewBag.Services = services;
            
            return View(vendor);
        }

        [HttpPost]
        public IActionResult PayBalance(string bookingId)
        {
            try
            {
                var customerId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(customerId))
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // Get the booking to verify it belongs to this customer
                var booking = _dataService.GetBooking(bookingId);
                if (booking == null || booking.CustomerId != customerId)
                {
                    TempData["ErrorMessage"] = "Booking not found or you don't have permission to pay for this booking.";
                    return RedirectToAction("Dashboard");
                }
                
                if (booking.Status != "Approved")
                {
                    TempData["ErrorMessage"] = "This booking is not approved yet. Please wait for vendor approval.";
                    return RedirectToAction("Dashboard");
                }
                
                if (booking.BalancePaidOnApp)
                {
                    TempData["InfoMessage"] = "Balance has already been paid for this booking.";
                    return RedirectToAction("Dashboard");
                }
                
                // Mark balance as paid - this updates BalancePaidOnApp to true and status to Confirmed
                _dataService.MarkBalanceAsPaid(bookingId);
                
                TempData["SuccessMessage"] = $"Balance of ₹{booking.BalanceAmount:N2} paid successfully! Your booking is now confirmed.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while processing the payment. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult CancelBooking(string bookingId)
        {
            try
            {
                var customerId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(customerId))
                {
                    return RedirectToAction("Login", "Account");
                }
                
                var booking = _dataService.GetBooking(bookingId);
                if (booking == null || booking.CustomerId != customerId)
                {
                    TempData["ErrorMessage"] = "Booking not found or access denied.";
                    return RedirectToAction("Dashboard");
                }
                
                if (booking.Status == "Cancelled" || booking.Status == "Rejected")
                {
                     TempData["InfoMessage"] = "Booking is already cancelled or rejected.";
                     return RedirectToAction("Dashboard");
                }

                // Calculate Refund
                decimal refundAmount = 0;
                if (booking.Status == "Confirmed" || booking.Status == "Approved" || booking.Status == "Requested")
                {
                    // Full refund of advance paid
                    refundAmount += booking.AdvancePaid;
                    
                    // If balance was paid on app, refund that too
                    if (booking.BalancePaidOnApp)
                    {
                        refundAmount += booking.BalanceAmount;
                    }
                }
                
                // Process Refund
                if (refundAmount > 0)
                {
                    _dataService.AddMoneyToWallet(customerId, refundAmount, $"Refund for Cancelled Booking #{bookingId.Substring(0, 8)}");
                }
                
                // Update Booking Status
                _dataService.UpdateBookingStatus(bookingId, "Cancelled", null, null);
                
                // Notify Vendor (Optional - for now just customer feedback)
                
                TempData["SuccessMessage"] = $"Booking cancelled successfully. Refund of ₹{refundAmount:N2} has been credited to your wallet.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while cancelling the booking.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult AddMoneyToWallet(decimal amount)
        {
            try
            {
                var customerId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(customerId))
                {
                    return RedirectToAction("Login", "Account");
                }
                
                if (amount <= 0)
                {
                    TempData["ErrorMessage"] = "Please enter a valid amount greater than zero.";
                    return RedirectToAction("Dashboard");
                }
                
                if (amount > 100000)
                {
                    TempData["ErrorMessage"] = "Maximum amount per transaction is ₹1,00,000.";
                    return RedirectToAction("Dashboard");
                }
                
                _dataService.AddMoneyToWallet(customerId, amount, "Wallet top-up");
                
                TempData["SuccessMessage"] = $"₹{amount:N2} added to your wallet successfully!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while adding money to wallet. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        private string GetOrCreateCookieId()
        {
            const string cookieName = "PartyClapCartId";
            if (Request.Cookies.ContainsKey(cookieName))
            {
                return Request.Cookies[cookieName];
            }
            
            var newId = Guid.NewGuid().ToString();
            Response.Cookies.Append(cookieName, newId, new CookieOptions { Expires = DateTime.Now.AddDays(30) });
            return newId;
        }
    }
}
