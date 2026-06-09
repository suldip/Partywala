using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PartyClap.Models;
using PartyClap.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartyClap.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IDataService _dataService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPricingService _pricingService;
        private readonly IOtpService _otpService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            IDataService dataService,
            IPasswordHasher passwordHasher,
            IPricingService pricingService,
            IOtpService otpService,
            INotificationService notificationService,
            ILogger<CustomerController> logger)
        {
            _dataService = dataService;
            _passwordHasher = passwordHasher;
            _pricingService = pricingService;
            _otpService = otpService;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register(string phone = null)
        {
            var model = new Customer
            {
                Phone = TempData["RegisterPhone"] as string ?? phone?.Trim() ?? "",
                Name = TempData["RegisterDraftName"] as string ?? "",
                Email = TempData["RegisterDraftEmail"] as string ?? ""
            };

            ViewBag.RegisterOtpSent = TempData["RegisterOtpSent"] as bool? == true;
            ViewBag.RegisterPhoneVerified = TempData["RegisterPhoneVerified"] as bool? == true;
            ViewBag.RegisterDebugOtp = TempData["RegisterDebugOtp"] as string;
            ViewBag.RegisterOtpError = TempData["RegisterOtpError"] as string;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer customer, string phoneOtp = null)
        {
            await HttpContext.Session.LoadAsync();

            if (ModelState.IsValid)
            {
                var phoneVerified = _otpService.IsPhoneVerified(customer.Phone);
                if (!phoneVerified && !string.IsNullOrWhiteSpace(phoneOtp))
                {
                    if (!_otpService.ValidateOtp(customer.Phone, phoneOtp, consume: false))
                    {
                        ModelState.AddModelError("Phone", "Invalid or expired OTP. Click Resend OTP and try again.");
                        ViewBag.RegisterOtpSent = true;
                        PopulateRegisterPageState(customer);
                        return View(customer);
                    }

                    _otpService.MarkPhoneVerified(customer.Phone);
                    phoneVerified = true;
                }

                if (!phoneVerified)
                {
                    ModelState.AddModelError("Phone", "Please enter the OTP and click Verify before creating your account.");
                    ViewBag.RegisterOtpSent = true;
                    PopulateRegisterPageState(customer);
                    return View(customer);
                }

                // Prevent duplicate accounts.
                if (_dataService.GetCustomerByPhone(customer.Phone) != null)
                {
                    ModelState.AddModelError("Phone", "An account already exists with this phone number. Please log in instead.");
                    return View(customer);
                }
                if (_dataService.GetCustomerByEmail(customer.Email) != null)
                {
                    ModelState.AddModelError("Email", "An account already exists with this email. Please log in instead.");
                    return View(customer);
                }
                if (_dataService.GetVendorByEmail(customer.Email) != null)
                {
                    ModelState.AddModelError("Email", "This email is registered to a vendor account. Please use a different email.");
                    return View(customer);
                }
                if (EmailRules.IsBlockedTestEmail(customer.Email))
                {
                    ModelState.AddModelError("Email", "Please use your real email address. Test or placeholder emails are not allowed.");
                    return View(customer);
                }

                customer.Id = Guid.NewGuid().ToString();
                if (!string.IsNullOrEmpty(customer.Password))
                {
                    customer.Password = _passwordHasher.Hash(customer.Password);
                }
                _dataService.RegisterCustomer(customer);

                _otpService.ClearPhoneVerification(customer.Phone);

                await HttpContext.SignInUserAsync("Customer", customer.Id, customer.Name);

                TempData["SuccessMessage"] = "Account created successfully! Welcome to PartyClap.";
                return RedirectToAction("Explore");
            }

            PopulateRegisterPageState(customer);
            return View(customer);
        }

        private void PopulateRegisterPageState(Customer customer)
        {
            if (customer == null)
            {
                return;
            }

            var verified = !string.IsNullOrWhiteSpace(customer.Phone) && _otpService.IsPhoneVerified(customer.Phone);
            ViewBag.RegisterPhoneVerified = verified;
            if (ViewBag.RegisterOtpSent as bool? != true)
            {
                ViewBag.RegisterOtpSent = verified || !string.IsNullOrWhiteSpace(customer.Phone);
            }
        }
        public IActionResult Dashboard(string section = "Overview", string chatWith = null, string pay = null, string requestId = null)
        {
            var customerId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

            if (!string.IsNullOrEmpty(pay))
            {
                return RedirectToAction("Payment", new { bookingId = pay });
            }

            if (!string.IsNullOrEmpty(requestId) && section == "ServiceRequests")
            {
                return RedirectToAction("PaymentCheckout");
            }
            
            var customer = _dataService.GetCustomerById(customerId);
            var bookings = _dataService.GetCustomerBookings(customerId);
            var serviceRequests = _dataService.GetCustomerServiceRequestsWithDetails(customerId);
            foreach (var request in serviceRequests)
            {
                if (request.TryGetValue("Status", out var statusObj)
                    && statusObj?.ToString() == "Approved"
                    && request.TryGetValue("ServiceCost", out var costObj)
                    && costObj != null)
                {
                    var pricing = _pricingService.CalculatePricing(Convert.ToDecimal(costObj));
                    request["PayAmount"] = pricing.CustomerTotalCost;
                }
            }
            var walletTransactions = _dataService.GetWalletTransactions(customerId, "Customer", 20);
            var addresses = _dataService.GetCustomerAddresses(customerId);
            
            // Loyalty calculation is delegated to the pricing service.
            var loyalty = _pricingService.CalculateLoyalty(bookings);

            PopulateMessagingViewBag(customerId, "Customer", chatWith);

            ViewBag.Customer = customer;
            ViewBag.ServiceRequests = serviceRequests;
            ViewBag.WalletTransactions = walletTransactions;
            ViewBag.Addresses = addresses;
            ViewBag.PartyPoints = loyalty.PartyPoints;
            ViewBag.LoyaltyTier = loyalty.Tier;
            ViewBag.NextTierPoints = loyalty.NextTierPoints;
            ViewBag.Section = section;
            ViewBag.ReviewedBookingIds = _dataService.GetReviewedBookingIds(customerId);
            
            return View(bookings);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(Customer customer)
        {
            var customerId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");
            
            customer.Id = customerId;
            _dataService.UpdateCustomerProfile(customer);
            
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Dashboard", new { section = "Profile" });
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult AddAddress(Address address)
        {
            var customerId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");
            
            if (address != null && !string.IsNullOrEmpty(address.PinCode) && !_dataService.IsPinCodeAllowed(address.PinCode))
            {
                TempData["ErrorMessage"] = "Booking is currently only available in our serviceable areas. Please check if your PIN is supported.";
                return RedirectToAction("Dashboard", new { section = "Addresses" });
            }

            address.Id = Guid.NewGuid().ToString();
            address.CustomerId = customerId;
            _dataService.AddAddress(address);
            
            TempData["SuccessMessage"] = "Address added successfully!";
            return RedirectToAction("Dashboard", new { section = "Addresses" });
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAddress(string addressId)
        {
            var customerId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");
            
            // Ownership is enforced in the data layer (delete scoped to CustomerId).
            _dataService.DeleteAddress(addressId, customerId);
            
            TempData["SuccessMessage"] = "Address deleted successfully!";
            return RedirectToAction("Dashboard", new { section = "Addresses" });
        }

        public IActionResult Explore(string search, string category, string pinCode, decimal? minPrice, decimal? maxPrice, decimal? minRating, string sortBy, DateTime? eventDate)
        {
            if (!string.IsNullOrEmpty(pinCode) && !_dataService.IsPinCodeAllowed(pinCode))
            {
                TempData["ErrorMessage"] = "Services are currently only available in our designated serviceable areas.";
                pinCode = null;
            }

            var normalizedCategory = ExploreFilterHelper.NormalizeCategory(category);
            var filterByDate = string.Equals(Request.Query["filterByDate"], "1", StringComparison.OrdinalIgnoreCase);
            DateTime? filterDate = null;

            if (filterByDate && eventDate.HasValue)
            {
                if (eventDate.Value.Date < DateTime.Today)
                {
                    TempData["ErrorMessage"] = "Party date must be today or a future date.";
                }
                else
                {
                    filterDate = eventDate.Value.Date;
                }
            }

            var services = _dataService.SearchServices(search, pinCode, minPrice, maxPrice, null, filterDate, normalizedCategory);
            var reviewSummaries = _dataService.GetServiceReviewSummaries(services.Select(s => s.Id));
            services = ExploreFilterHelper.ApplyRatingAndSort(services, minRating, sortBy, reviewSummaries);

            ViewBag.ReviewSummaries = reviewSummaries;
            ViewBag.DateFilterActive = filterDate.HasValue;
            
            ViewBag.Search = search;
            ViewBag.Category = normalizedCategory;
            ViewBag.PinCode = pinCode;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.MinRating = minRating;
            ViewBag.SortBy = string.IsNullOrWhiteSpace(sortBy) ? "rating" : sortBy;
            ViewBag.EventDate = (eventDate?.ToString("yyyy-MM-dd")) ?? DateTime.Today.ToString("yyyy-MM-dd");

            var scheduleFrom = DateTime.Today;
            var scheduleTo = DateTime.Today.AddDays(41);
            var vendorSchedules = new Dictionary<string, List<VendorScheduleEntry>>();
            foreach (var vendorId in services.Select(s => s.VendorId).Distinct())
            {
                vendorSchedules[vendorId] = _dataService.GetVendorSchedule(vendorId, scheduleFrom, scheduleTo);
            }
            ViewBag.VendorSchedules = vendorSchedules;
            ViewBag.ScheduleFrom = scheduleFrom.ToString("yyyy-MM-dd");
            ViewBag.ScheduleTo = scheduleTo.ToString("yyyy-MM-dd");
            
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
                var customerId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(customerId))
                {
                    return Json(new { success = false, message = "Please login to request a service" });
                }

                // Check if user is a Vendor
                var role = HttpContext.GetUserRole();
                if (role == "Vendor")
                {
                    // Optionally: could try to check if vendor exists in customer table, but safer to block for now to explain "notacces"
                    return Json(new { success = false, message = "Vendor accounts cannot request services. Please login with a Customer account." });
                }

                // Get service to get vendor ID
                var service = _dataService.GetService(request.ServiceId);
                if (service == null)
                {
                    return Json(new { success = false, message = "Service not found" });
                }

                var eventEndDate = request.EventEndDate?.Date ?? request.EventDate.Date;
                if (request.EventDate.Date < DateTime.Today || eventEndDate < request.EventDate.Date)
                {
                    return Json(new { success = false, message = "Please select a valid event start and end date." });
                }

                if (string.IsNullOrWhiteSpace(request.EventStartTime) || string.IsNullOrWhiteSpace(request.EventEndTime))
                {
                    return Json(new { success = false, message = "Please set event start and end time." });
                }

                if (eventEndDate == request.EventDate.Date
                    && string.Compare(request.EventEndTime, request.EventStartTime, StringComparison.Ordinal) <= 0)
                {
                    return Json(new { success = false, message = "End time must be after start time on the same day." });
                }

                if (!_dataService.IsVendorAvailableForRange(service.VendorId, request.EventDate, eventEndDate))
                {
                    return Json(new { success = false, message = "This vendor is not available on one or more dates in your selected range." });
                }

                var dayCount = _pricingService.CountEventDays(request.EventDate, eventEndDate);
                var totalCost = _pricingService.CalculateMultiDayCost(
                    service.Cost, service.WeekendCost, request.EventDate, eventEndDate);

                var serviceRequest = new ServiceRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerId,
                    VendorId = service.VendorId,
                    ServiceId = request.ServiceId,
                    EventDate = request.EventDate,
                    EventEndDate = eventEndDate,
                    EventStartTime = request.EventStartTime,
                    EventEndTime = request.EventEndTime,
                    DayCount = dayCount,
                    TotalCost = totalCost,
                    EventType = request.EventType,
                    GuestCount = request.GuestCount,
                    AdditionalDetails = request.AdditionalDetails,
                    PartyLocation = request.PartyLocation?.Trim(),
                    PartyPinCode = request.PartyPinCode?.Trim(),
                    PartyLatitude = request.PartyLatitude,
                    PartyLongitude = request.PartyLongitude,
                    Status = "UnderProcess",
                    CreatedDate = DateTime.Now
                };

                _dataService.CreateServiceRequest(serviceRequest);

                var customer = _dataService.GetCustomerById(customerId);
                var vendor = _dataService.GetVendor(service.VendorId);
                var timeLabel = $"{request.EventStartTime}–{request.EventEndTime}";
                var dateLabel = dayCount > 1
                    ? $"{request.EventDate:MMM dd, yyyy} – {eventEndDate:MMM dd, yyyy} ({dayCount} days) · {timeLabel}"
                    : $"{request.EventDate:MMM dd, yyyy} · {timeLabel}";
                _notificationService.CreateNotification(new Notification
                {
                    UserId = service.VendorId,
                    UserType = "Vendor",
                    Title = "New Service Request",
                    Message = $"{customer?.Name ?? "A customer"} requested {service.ServiceType} for {dateLabel}. Total: ₹{totalCost:N0}.",
                    Type = "ServiceRequest",
                    RelatedId = serviceRequest.Id,
                    Icon = "📩",
                    ActionUrl = "/PartyClap/Vendor/Dashboard?section=servicerequests"
                });

                return Json(new
                {
                    success = true,
                    message = $"Service request submitted for {dayCount} day(s)! Total ₹{totalCost:N0}. Track it in your Pending cart while the vendor reviews.",
                    eventDate = request.EventDate.ToString("yyyy-MM-dd"),
                    eventEndDate = eventEndDate.ToString("yyyy-MM-dd"),
                    dayCount,
                    totalCost
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error submitting request. Please try again." });
            }
        }

        public class ServiceRequestRequest
        {
            public string ServiceId { get; set; }
            public string VendorId { get; set; }
            public DateTime EventDate { get; set; }
            public DateTime? EventEndDate { get; set; }
            public string EventStartTime { get; set; }
            public string EventEndTime { get; set; }
            public string EventType { get; set; }
            public int GuestCount { get; set; }
            public string AdditionalDetails { get; set; }
            public string PartyLocation { get; set; }
            public string PartyPinCode { get; set; }
            public decimal? PartyLatitude { get; set; }
            public decimal? PartyLongitude { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public IActionResult AddToCart(string serviceId, string vendorId, DateTime? eventDate)
        {
            var service = _dataService.GetService(serviceId);
            if (service == null || service.Cost < ServicePricingRules.MinimumCost)
            {
                TempData["ErrorMessage"] = "This service is not available for booking until the vendor sets a valid price.";
                return RedirectToAction("Explore");
            }

            var (customerId, cookieId) = GetCartContext();
            MergeGuestCartIfNeeded(customerId, cookieId);
            _dataService.AddToCart(customerId, cookieId, serviceId, vendorId, eventDate);
            return RedirectToAction("ViewCart");
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public IActionResult AddToCartJson(string serviceId, string vendorId, DateTime? eventDate,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null)
        {
            try
            {
                var service = _dataService.GetService(serviceId);
                if (service == null || service.Cost < ServicePricingRules.MinimumCost)
                {
                    return Json(new { success = false, message = "This service is not available until the vendor sets a valid price." });
                }

                if (!eventDate.HasValue || eventDate.Value.Date < DateTime.Today)
                {
                    return Json(new { success = false, message = "Please select an available (green) date from the calendar before booking." });
                }

                if (!_dataService.IsVendorAvailableOnDate(vendorId, eventDate.Value))
                {
                    return Json(new { success = false, message = "This vendor is booked on that date. Please choose a green available date." });
                }

                var (customerId, cookieId) = GetCartContext();
                MergeGuestCartIfNeeded(customerId, cookieId);
                _dataService.AddToCart(customerId, cookieId, serviceId, vendorId, eventDate,
                    partyLocation?.Trim(), partyPinCode?.Trim(), partyLatitude, partyLongitude);
                var totalCount = GetCartTotalCount(customerId, cookieId);
                return Json(new { success = true, count = totalCount, totalCount, message = "Added to cart" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Could not add item to cart." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Customer")]
        public IActionResult GetCartJson()
        {
            var (customerId, cookieId) = GetCartContext();
            MergeGuestCartIfNeeded(customerId, cookieId);
            var items = _dataService.GetCartItems(customerId, cookieId);
            var totalCount = GetCartTotalCount(customerId, cookieId, items);
            var (pending, accepted) = BuildCartRequestSections(customerId);
            return Json(new
            {
                success = true,
                items = items,
                count = items.Count,
                pendingCount = pending.Count,
                acceptedCount = accepted.Count,
                totalCount
            });
        }

        [Authorize(Roles = "Customer")]
        public IActionResult ViewCart()
        {
            var (customerId, cookieId) = GetCartContext();
            MergeGuestCartIfNeeded(customerId, cookieId);
            var items = _dataService.GetCartItems(customerId, cookieId);
            EnrichCartItems(items);
            var expiredNotices = ProcessTimedOutPendingRequests(customerId);
            var (pending, accepted) = BuildCartRequestSections(customerId);
            var checkoutPreview = BuildPaymentCheckoutViewModel(customerId, accepted);
            if (expiredNotices.Count > 0)
            {
                TempData["InfoMessage"] = expiredNotices.Count == 1
                    ? expiredNotices[0]
                    : $"{expiredNotices.Count} requests timed out waiting for vendor response. You can choose another vendor.";
            }

            return View("Cart", new CartPageViewModel
            {
                Items = items,
                PendingRequests = pending,
                AcceptedRequests = checkoutPreview.Items,
                AcceptedSubtotal = checkoutPreview.Subtotal,
                AcceptedGstAmount = checkoutPreview.GstAmount,
                AcceptedGrandTotal = checkoutPreview.GrandTotal,
                GstPercent = checkoutPreview.GstPercent,
                ExpiredVendorNotices = expiredNotices
            });
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public IActionResult RemoveFromCart(int cartItemId)
        {
            _dataService.RemoveFromCart(cartItemId);
            return RedirectToAction("ViewCart");
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveCartRequest(string id, string source)
        {
            var customerId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(source))
            {
                TempData["ErrorMessage"] = "Invalid cart item.";
                return RedirectToAction("ViewCart");
            }

            try
            {
                if (source.Equals("ServiceRequest", StringComparison.OrdinalIgnoreCase))
                {
                    var requests = _dataService.GetCustomerServiceRequestsWithDetails(customerId);
                    var request = requests.FirstOrDefault(r => r["Id"]?.ToString() == id);
                    if (request == null)
                    {
                        TempData["ErrorMessage"] = "Request not found.";
                        return RedirectToAction("ViewCart");
                    }

                    var status = request["Status"]?.ToString()?.Trim() ?? "";
                    if (CartTerminalServiceRequestStatuses.Contains(status))
                    {
                        TempData["InfoMessage"] = "This request can no longer be removed.";
                        return RedirectToAction("ViewCart");
                    }

                    _dataService.UpdateServiceRequestStatus(id, "Cancelled");
                    TempData["SuccessMessage"] = "Request removed from your cart.";
                }
                else if (source.Equals("Booking", StringComparison.OrdinalIgnoreCase))
                {
                    var booking = _dataService.GetBooking(id);
                    if (booking == null || booking.CustomerId != customerId)
                    {
                        TempData["ErrorMessage"] = "Booking not found.";
                        return RedirectToAction("ViewCart");
                    }

                    if (booking.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)
                        || (booking.AdvancePaid > 0 && booking.AdvancePaid >= booking.CustomerTotalCost))
                    {
                        TempData["ErrorMessage"] = "Paid bookings cannot be removed here.";
                        return RedirectToAction("ViewCart");
                    }

                    if (booking.AdvancePaid > 0)
                    {
                        _dataService.AddMoneyToWallet(customerId, booking.AdvancePaid,
                            $"Refund for removed booking #{id.Substring(0, 8)}");
                    }

                    _dataService.UpdateBookingStatus(id, "Cancelled", null, null);
                    TempData["SuccessMessage"] = "Booking removed from your cart.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid cart item type.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveCartRequest failed for {Source} {Id}", source, id);
                TempData["ErrorMessage"] = "Could not remove item. Please try again.";
            }

            return RedirectToAction("ViewCart");
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public IActionResult UpdateCartItemDate([FromBody] UpdateCartItemDateRequest request)
        {
            try
            {
                var (customerId, cookieId) = GetCartContext();
                var items = _dataService.GetCartItems(customerId, cookieId);
                var cartItem = items.FirstOrDefault(i => i.Id == request.CartItemId);
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found." });
                }

                var eventEndDate = request.EventEndDate?.Date ?? request.EventDate?.Date;
                _dataService.UpdateCartItemSchedule(request.CartItemId, request.EventDate, eventEndDate, request.EventStartTime, request.EventEndTime,
                    request.PartyLocation?.Trim(), request.PartyPinCode?.Trim(), request.PartyLatitude, request.PartyLongitude);
                
                items = _dataService.GetCartItems(customerId, cookieId);
                var item = items.FirstOrDefault(i => i.Id == request.CartItemId);
                EnrichCartItems(items);
                item = items.FirstOrDefault(i => i.Id == request.CartItemId);

                var dayCount = item?.EventDayCount ?? 1;
                var lineTotal = item?.LineTotal ?? 0m;
                var orderTotal = items.Sum(i => i.LineTotal > 0 ? i.LineTotal : i.Cost);

                return Json(new
                {
                    success = true,
                    recalculate = request.EventDate.HasValue,
                    dayCount,
                    lineTotal,
                    orderTotal
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Could not update the cart item." });
            }
        }
        
        public class UpdateCartItemDateRequest
        {
            public int CartItemId { get; set; }
            public DateTime? EventDate { get; set; }
            public DateTime? EventEndDate { get; set; }
            public string EventStartTime { get; set; }
            public string EventEndTime { get; set; }
            public string PartyLocation { get; set; }
            public string PartyPinCode { get; set; }
            public decimal? PartyLatitude { get; set; }
            public decimal? PartyLongitude { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout()
        {
            var (customerId, cookieId) = GetCartContext();
            if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

            MergeGuestCartIfNeeded(customerId, cookieId);
            var items = _dataService.GetCartItems(customerId, cookieId);
            
            if (items.Count == 0) return RedirectToAction("ViewCart");

            EnrichCartItems(items);

            foreach (var item in items)
            {
                var eventEndDate = item.EventEndDate ?? item.EventDate;
                if (!item.EventDate.HasValue || !eventEndDate.HasValue
                    || string.IsNullOrWhiteSpace(item.EventStartTime)
                    || string.IsNullOrWhiteSpace(item.EventEndTime))
                {
                    TempData["ErrorMessage"] = "Please set the event start date, end date, start time, and end time for every cart item before checkout.";
                    return RedirectToAction("ViewCart");
                }

                if (eventEndDate.Value.Date < item.EventDate.Value.Date)
                {
                    TempData["ErrorMessage"] = "Event end date cannot be before the start date.";
                    return RedirectToAction("ViewCart");
                }

                if (string.Compare(item.EventEndTime, item.EventStartTime, StringComparison.Ordinal) <= 0)
                {
                    TempData["ErrorMessage"] = "End time must be after start time for every cart item.";
                    return RedirectToAction("ViewCart");
                }

                if (!_dataService.IsVendorAvailableForRange(item.VendorId, item.EventDate.Value, eventEndDate.Value))
                {
                    TempData["ErrorMessage"] = "One or more vendors are no longer available on your selected dates. Please update your cart.";
                    return RedirectToAction("ViewCart");
                }
            }

            var customer = _dataService.GetCustomerById(customerId);

            foreach (var item in items)
            {
                var eventEndDate = item.EventEndDate ?? item.EventDate.Value;
                var lineTotal = _pricingService.CalculateCartItemTotal(item);
                var dayCount = _pricingService.CountEventDays(item.EventDate.Value, eventEndDate);

                var serviceRequest = new ServiceRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerId,
                    VendorId = item.VendorId,
                    ServiceId = item.ServiceId,
                    EventDate = item.EventDate.Value,
                    EventEndDate = eventEndDate,
                    EventStartTime = item.EventStartTime,
                    EventEndTime = item.EventEndTime,
                    DayCount = dayCount,
                    TotalCost = lineTotal,
                    EventType = "Party Event",
                    GuestCount = 0,
                    PartyLocation = item.PartyLocation?.Trim(),
                    PartyPinCode = item.PartyPinCode?.Trim(),
                    PartyLatitude = item.PartyLatitude,
                    PartyLongitude = item.PartyLongitude,
                    Status = "UnderProcess",
                    CreatedDate = DateTime.Now
                };

                _dataService.CreateServiceRequest(serviceRequest);

                var vendor = _dataService.GetVendor(item.VendorId);
                var service = _dataService.GetService(item.ServiceId);
                var timeLabel = $"{item.EventStartTime}–{item.EventEndTime}";
                var dateLabel = dayCount > 1
                    ? $"{item.EventDate.Value:MMM dd, yyyy} – {eventEndDate:MMM dd, yyyy} ({dayCount} days) · {timeLabel}"
                    : $"{item.EventDate.Value:MMM dd, yyyy} · {timeLabel}";
                _notificationService.CreateNotification(new Notification
                {
                    UserId = item.VendorId,
                    UserType = "Vendor",
                    Title = "New Service Request",
                    Message = $"{customer?.Name ?? "A customer"} requested {service?.ServiceType ?? "a service"} for {dateLabel}. Total: ₹{lineTotal:N0}.",
                    Type = "ServiceRequest",
                    RelatedId = serviceRequest.Id,
                    Icon = "📩",
                    ActionUrl = "/PartyClap/Vendor/Dashboard?section=servicerequests"
                });
            }

            _dataService.ClearCart(customerId, cookieId);

            TempData["SuccessMessage"] = "Your request was sent to the vendor. Track it in Pending cart — vendors have 2 hours to respond. Pay after approval from Accepted cart.";
            return RedirectToAction("ViewCart");
        }
        
        public IActionResult VendorProfile(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
            {
                return VendorNotFound();
            }

            var vendor = _dataService.GetVendor(vendorId);
            if (vendor == null)
            {
                return VendorNotFound();
            }
            
            var portfolio = _dataService.GetVendorPortfolio(vendorId);
            var services = _dataService.GetVendorServices(vendorId);
            var reviews = _dataService.GetVendorReviews(vendorId);
            var reviewSummary = _dataService.GetVendorReviewSummary(vendorId);
            
            vendor.Services = services; 
            
            ViewBag.Portfolio = portfolio;
            ViewBag.Services = services;
            ViewBag.Reviews = reviews;
            ViewBag.ReviewSummary = reviewSummary;

            var scheduleFrom = DateTime.Today;
            var scheduleTo = DateTime.Today.AddDays(41);
            ViewBag.VendorSchedule = _dataService.GetVendorSchedule(vendorId, scheduleFrom, scheduleTo);
            ViewBag.ScheduleFrom = scheduleFrom.ToString("yyyy-MM-dd");
            ViewBag.ScheduleTo = scheduleTo.ToString("yyyy-MM-dd");
            ViewBag.EventDate = Request.Query["eventDate"].FirstOrDefault() ?? "";
            
            return View(vendor);
        }

        private IActionResult VendorNotFound()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            ViewData["Message"] = "This vendor profile could not be found. The link may be invalid or the vendor may no longer be on PartyClap.";
            return View("NotFound");
        }

        [Authorize(Roles = "Customer")]
        public IActionResult PaymentCheckout()
        {
            var customerId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

            var checkout = BuildPaymentCheckoutViewModel(customerId);
            if (!checkout.Items.Any())
            {
                TempData["InfoMessage"] = "No approved bookings ready for payment.";
                return RedirectToAction("ViewCart");
            }

            return View(checkout);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult PayAcceptedCart()
        {
            try
            {
                var customerId = HttpContext.GetUserId();
                if (string.IsNullOrEmpty(customerId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var checkout = BuildPaymentCheckoutViewModel(customerId);
                if (!checkout.Items.Any())
                {
                    TempData["ErrorMessage"] = "No approved bookings to pay.";
                    return RedirectToAction("ViewCart");
                }

                var paidCount = 0;
                var totalPaid = 0m;

                foreach (var item in checkout.Items)
                {
                    if (item.Source == "ServiceRequest")
                    {
                        var paid = TryPayServiceRequest(customerId, item.Id, out var amount);
                        if (paid)
                        {
                            paidCount++;
                            totalPaid += amount;
                        }
                    }
                    else if (item.Source == "Booking")
                    {
                        var paid = TryPayApprovedBooking(customerId, item.Id, out var amount);
                        if (paid)
                        {
                            paidCount++;
                            totalPaid += amount;
                        }
                    }
                }

                if (paidCount == 0)
                {
                    TempData["ErrorMessage"] = "Could not process payment. Please try again.";
                    return RedirectToAction("PaymentCheckout");
                }

                TempData["SuccessMessage"] = paidCount == 1
                    ? $"Payment of ₹{totalPaid:N0} (incl. GST) completed! Your booking is confirmed."
                    : $"Payment of ₹{totalPaid:N0} (incl. GST) completed for {paidCount} bookings!";
                return RedirectToAction("ViewCart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayAcceptedCart failed");
                TempData["ErrorMessage"] = "Could not process payment. Please try again.";
                return RedirectToAction("PaymentCheckout");
            }
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Payment(string bookingId)
        {
            var customerId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(bookingId))
            {
                TempData["ErrorMessage"] = "Invalid payment link.";
                return RedirectToAction("Dashboard", new { section = "Bookings" });
            }

            var booking = _dataService.GetBooking(bookingId);
            if (booking == null || booking.CustomerId != customerId)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("Dashboard", new { section = "Bookings" });
            }

            var amountDue = booking.CustomerTotalCost - booking.AdvancePaid;
            var canPay = booking.Status == "Approved" && amountDue > 0;

            ViewBag.Vendor = _dataService.GetVendor(booking.VendorId);
            ViewBag.Service = _dataService.GetService(booking.ServiceId);
            ViewBag.AmountDue = amountDue;
            ViewBag.CanPay = canPay;
            ViewBag.DayCount = booking.EventEndDate.HasValue
                ? _pricingService.CountEventDays(booking.EventDate, booking.EventEndDate.Value)
                : 1;

            return RedirectToAction("PaymentCheckout");
        }

        [Authorize(Roles = "Customer")]
        public IActionResult PaymentRequest(string requestId)
        {
            return RedirectToAction("PaymentCheckout");
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Invoice(string bookingId)
        {
            var customerId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(customerId)) return RedirectToAction("Login", "Account");

            var booking = _dataService.GetBooking(bookingId);
            if (booking == null || booking.CustomerId != customerId)
            {
                TempData["ErrorMessage"] = "Invoice not found or you don't have permission to view it.";
                return RedirectToAction("Dashboard");
            }

            ViewBag.Vendor = _dataService.GetVendor(booking.VendorId);
            ViewBag.Service = _dataService.GetService(booking.ServiceId);
            ViewBag.Customer = _dataService.GetCustomerById(customerId);
            return View(booking);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult PayServiceRequest(string requestId)
        {
            return PayAcceptedCart();
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult PayBookingAdvance(string bookingId)
        {
            return PayAcceptedCart();
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult PayBalance(string bookingId)
        {
            try
            {
                var customerId = HttpContext.GetUserId();
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
                
                if (booking.Status != "Confirmed" || booking.AdvancePaid <= 0)
                {
                    TempData["ErrorMessage"] = "Pay the advance first after vendor approval, then you can pay the remaining balance.";
                    return RedirectToAction("Dashboard", new { section = "Bookings" });
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
                _logger.LogError(ex, "PayBalance failed for booking {BookingId}", bookingId);
                TempData["ErrorMessage"] = "An error occurred while processing the payment. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult CancelBooking(string bookingId)
        {
            try
            {
                var customerId = HttpContext.GetUserId();
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
                _logger.LogError(ex, "CancelBooking failed for booking {BookingId}", bookingId);
                TempData["ErrorMessage"] = "An error occurred while cancelling the booking.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public IActionResult AddMoneyToWallet(decimal amount)
        {
            try
            {
                var customerId = HttpContext.GetUserId();
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
                _logger.LogError(ex, "AddMoneyToWallet failed for amount {Amount}", amount);
                TempData["ErrorMessage"] = "An error occurred while adding money to wallet. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        private const int VendorResponseTimeoutHours = 2;

        private static readonly HashSet<string> CartTerminalServiceRequestStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Paid", "Completed", "Cancelled", "Expired"
        };

        private static readonly HashSet<string> AwaitingVendorResponseStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "UnderProcess", "Under Process", "Pending", "Requested"
        };

        private static readonly HashSet<string> CartTerminalBookingStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Confirmed", "Completed", "Cancelled"
        };

        private static string NormalizeCartRequestStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "Pending";
            var trimmed = status.Trim();
            if (trimmed.Equals("UnderProcess", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Under Process", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Requested", StringComparison.OrdinalIgnoreCase))
            {
                return "Pending";
            }

            return trimmed;
        }

        private int GetCartTotalCount(string customerId, string cookieId, List<CartItem> items = null)
        {
            if (!string.IsNullOrEmpty(customerId))
            {
                ProcessTimedOutPendingRequests(customerId);
            }

            items ??= _dataService.GetCartItems(customerId, cookieId);
            if (string.IsNullOrEmpty(customerId))
            {
                return items.Count;
            }

            var (pending, accepted) = BuildCartRequestSections(customerId);
            return items.Count + pending.Count + accepted.Count;
        }

        private bool IsAwaitingVendorResponse(string status)
            => !string.IsNullOrWhiteSpace(status) && AwaitingVendorResponseStatuses.Contains(status.Trim());

        private bool HasVendorResponseTimedOut(DateTime createdDate, DateTime? responseDate, string status)
        {
            if (!IsAwaitingVendorResponse(status)) return false;
            if (responseDate.HasValue) return false;
            return DateTime.Now >= createdDate.AddHours(VendorResponseTimeoutHours);
        }

        private List<string> ProcessTimedOutPendingRequests(string customerId)
        {
            var notices = new List<string>();
            if (string.IsNullOrWhiteSpace(customerId)) return notices;

            foreach (var request in _dataService.GetCustomerServiceRequestsWithDetails(customerId))
            {
                var status = request["Status"]?.ToString()?.Trim() ?? "";
                if (!IsAwaitingVendorResponse(status)) continue;

                var created = request.ContainsKey("CreatedDate")
                    ? Convert.ToDateTime(request["CreatedDate"])
                    : DateTime.Now;
                DateTime? responseDate = request.ContainsKey("ResponseDate") && request["ResponseDate"] != null
                    ? Convert.ToDateTime(request["ResponseDate"])
                    : (DateTime?)null;

                if (!HasVendorResponseTimedOut(created, responseDate, status)) continue;

                var id = request["Id"]?.ToString();
                if (string.IsNullOrEmpty(id)) continue;

                _dataService.UpdateServiceRequestStatus(id, "Expired");
                var vendorName = request["VendorName"]?.ToString() ?? "The vendor";
                var serviceType = request["ServiceType"]?.ToString() ?? "service";
                notices.Add($"{vendorName} did not accept or decline your {serviceType} request within 2 hours. You can choose another vendor.");
            }

            foreach (var booking in _dataService.GetCustomerBookings(customerId))
            {
                var status = booking.Status?.Trim() ?? "";
                if (!IsAwaitingVendorResponse(status)) continue;
                if (!HasVendorResponseTimedOut(booking.BookingDate, null, status)) continue;

                _dataService.UpdateBookingStatus(booking.Id, "Cancelled", null, null);
                var vendor = _dataService.GetVendor(booking.VendorId);
                var service = _dataService.GetService(booking.ServiceId);
                notices.Add($"{vendor?.Name ?? "The vendor"} did not accept or decline your {service?.ServiceType ?? "service"} request within 2 hours. You can choose another vendor.");
            }

            return notices;
        }

        private (List<CartRequestItem> pending, List<CartRequestItem> accepted) BuildCartRequestSections(string customerId)
        {
            var pending = new List<CartRequestItem>();
            var accepted = new List<CartRequestItem>();
            if (string.IsNullOrWhiteSpace(customerId)) return (pending, accepted);

            foreach (var request in _dataService.GetCustomerServiceRequestsWithDetails(customerId))
            {
                var status = request["Status"]?.ToString()?.Trim() ?? "Pending";
                if (CartTerminalServiceRequestStatuses.Contains(status))
                {
                    continue;
                }

                var item = MapServiceRequestToCartItem(request);
                item.Status = NormalizeCartRequestStatus(status);
                if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                {
                    accepted.Add(item);
                }
                else
                {
                    pending.Add(item);
                }
            }

            foreach (var booking in _dataService.GetCustomerBookings(customerId))
            {
                var bookingStatus = booking.Status?.Trim() ?? "";
                if (CartTerminalBookingStatuses.Contains(bookingStatus))
                {
                    continue;
                }

                var service = _dataService.GetService(booking.ServiceId);
                var vendor = _dataService.GetVendor(booking.VendorId);
                var endDate = booking.EventEndDate ?? booking.EventDate;
                var dayCount = _pricingService.CountEventDays(booking.EventDate, endDate);
                var item = new CartRequestItem
                {
                    Id = booking.Id,
                    Source = "Booking",
                    VendorName = vendor?.Name ?? "Vendor",
                    ServiceType = service?.ServiceType ?? "Service",
                    EventDate = booking.EventDate,
                    EventEndDate = booking.EventEndDate,
                    EventStartTime = booking.EventStartTime,
                    EventEndTime = booking.EventEndTime,
                    DayCount = dayCount,
                    TotalCost = booking.VendorCost > 0 ? booking.VendorCost : booking.CustomerTotalCost,
                    Status = NormalizeCartRequestStatus(bookingStatus),
                    PaymentUrl = Url.Action("PaymentCheckout", "Customer")
                };

                if (bookingStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                    && booking.AdvancePaid < booking.CustomerTotalCost)
                {
                    accepted.Add(item);
                }
                else
                {
                    pending.Add(item);
                }
            }

            pending = pending.OrderByDescending(r => r.EventDate).ToList();
            accepted = accepted.OrderByDescending(r => r.EventDate).ToList();
            return (pending, accepted);
        }

        private CartRequestItem MapServiceRequestToCartItem(Dictionary<string, object> request)
        {
            var id = request["Id"].ToString();
            var start = (DateTime)request["EventDate"];
            var end = request.ContainsKey("EventEndDate") && request["EventEndDate"] != null
                ? (DateTime?)request["EventEndDate"]
                : null;
            var dayCount = request.ContainsKey("DayCount") ? Convert.ToInt32(request["DayCount"]) : 1;

            return new CartRequestItem
            {
                Id = id,
                Source = "ServiceRequest",
                VendorName = request["VendorName"]?.ToString() ?? "Vendor",
                ServiceType = request["ServiceType"]?.ToString() ?? "Service",
                EventDate = start,
                EventEndDate = end,
                EventStartTime = request.ContainsKey("EventStartTime") ? request["EventStartTime"]?.ToString() : null,
                EventEndTime = request.ContainsKey("EventEndTime") ? request["EventEndTime"]?.ToString() : null,
                DayCount = dayCount,
                TotalCost = request.ContainsKey("ServiceCost") ? Convert.ToDecimal(request["ServiceCost"]) : 0m,
                Status = request["Status"]?.ToString() ?? "",
                PartyLocation = request.ContainsKey("PartyLocation") ? request["PartyLocation"]?.ToString() : null,
                PartyPinCode = request.ContainsKey("PartyPinCode") ? request["PartyPinCode"]?.ToString() : null,
                PaymentUrl = Url.Action("PaymentCheckout", "Customer")
            };
        }

        private PaymentCheckoutViewModel BuildPaymentCheckoutViewModel(string customerId, List<CartRequestItem> accepted = null)
        {
            accepted ??= BuildCartRequestSections(customerId).accepted;
            var gstPercent = _pricingService.GetGstPercent();
            decimal subtotal = 0m;
            decimal gstTotal = 0m;

            foreach (var item in accepted)
            {
                var baseCost = GetCheckoutBaseCost(item);
                var pricing = _pricingService.CalculatePricing(baseCost);
                item.GstAmount = pricing.GstAmount;
                item.GrandTotal = pricing.CustomerTotalCost;
                subtotal += pricing.Subtotal;
                gstTotal += pricing.GstAmount;
            }

            return new PaymentCheckoutViewModel
            {
                Items = accepted,
                Subtotal = subtotal,
                GstPercent = gstPercent,
                GstAmount = gstTotal,
                GrandTotal = subtotal + gstTotal
            };
        }

        private decimal GetCheckoutBaseCost(CartRequestItem item)
        {
            if (item.Source == "Booking")
            {
                var booking = _dataService.GetBooking(item.Id);
                if (booking != null && booking.VendorCost > 0)
                {
                    return booking.VendorCost;
                }
            }

            return item.TotalCost;
        }

        private bool TryPayServiceRequest(string customerId, string requestId, out decimal amountPaid)
        {
            amountPaid = 0m;
            var requests = _dataService.GetCustomerServiceRequestsWithDetails(customerId);
            var request = requests.FirstOrDefault(r => r["Id"].ToString() == requestId);
            if (request == null || request["Status"].ToString() != "Approved")
            {
                return false;
            }

            var serviceCost = Convert.ToDecimal(request["ServiceCost"]);
            if (serviceCost < ServicePricingRules.MinimumCost)
            {
                return false;
            }

            var pricing = _pricingService.CalculatePricing(serviceCost);
            var eventEndDate = request.ContainsKey("EventEndDate") && request["EventEndDate"] != null
                ? (DateTime?)Convert.ToDateTime(request["EventEndDate"])
                : Convert.ToDateTime(request["EventDate"]);

            var booking = new Booking
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                VendorId = request["VendorId"].ToString(),
                ServiceId = request["ServiceId"].ToString(),
                VendorCost = pricing.VendorCost,
                CustomerTotalCost = pricing.CustomerTotalCost,
                AdvancePaid = pricing.CustomerTotalCost,
                BalanceAmount = 0,
                EventDate = Convert.ToDateTime(request["EventDate"]),
                EventEndDate = eventEndDate,
                EventStartTime = request.ContainsKey("EventStartTime") && request["EventStartTime"] != null
                    ? request["EventStartTime"].ToString() : "10:00",
                EventEndTime = request.ContainsKey("EventEndTime") && request["EventEndTime"] != null
                    ? request["EventEndTime"].ToString() : "18:00",
                PartyLocation = request.ContainsKey("PartyLocation") ? request["PartyLocation"]?.ToString()?.Trim() : null,
                PartyPinCode = request.ContainsKey("PartyPinCode") ? request["PartyPinCode"]?.ToString()?.Trim() : null,
                PartyLatitude = request.ContainsKey("PartyLatitude") && request["PartyLatitude"] != null
                    ? Convert.ToDecimal(request["PartyLatitude"]) : (decimal?)null,
                PartyLongitude = request.ContainsKey("PartyLongitude") && request["PartyLongitude"] != null
                    ? Convert.ToDecimal(request["PartyLongitude"]) : (decimal?)null,
                Status = "Confirmed",
                BalancePaidOnApp = true
            };

            _dataService.AddBooking(booking);
            _dataService.UpdateBookingSchedule(booking.Id, booking.EventStartTime, booking.EventEndTime, eventEndDate,
                booking.PartyLocation, booking.PartyPinCode, booking.PartyLatitude, booking.PartyLongitude);
            _dataService.UpdateServiceRequestStatus(requestId, "Paid");

            _notificationService.CreateNotification(new Notification
            {
                UserId = booking.VendorId,
                UserType = "Vendor",
                Title = "Payment Received",
                Message = $"Full payment of ₹{booking.AdvancePaid:N0} (incl. GST) received for {request["ServiceType"]} on {booking.EventDate:MMM dd, yyyy}.",
                Type = "ServiceRequestPaid",
                RelatedId = booking.Id,
                Icon = "💰",
                ActionUrl = "/PartyClap/Vendor/Dashboard?section=bookings"
            });

            amountPaid = booking.AdvancePaid;
            return true;
        }

        private bool TryPayApprovedBooking(string customerId, string bookingId, out decimal amountPaid)
        {
            amountPaid = 0m;
            var booking = _dataService.GetBooking(bookingId);
            if (booking == null || booking.CustomerId != customerId || booking.Status != "Approved")
            {
                return false;
            }

            if (booking.AdvancePaid >= booking.CustomerTotalCost && booking.BalancePaidOnApp)
            {
                return false;
            }

            var pricing = _pricingService.CalculatePricing(booking.VendorCost);
            _dataService.UpdateBookingStatus(bookingId, "Approved", pricing.VendorCost, pricing.CustomerTotalCost);
            _dataService.MarkAdvanceAsPaid(bookingId);
            booking = _dataService.GetBooking(bookingId);

            _notificationService.CreateNotification(new Notification
            {
                UserId = booking.VendorId,
                UserType = "Vendor",
                Title = "Payment Received",
                Message = $"Full payment of ₹{booking.AdvancePaid:N0} (incl. GST) received for {booking.EventDate:MMM dd, yyyy}.",
                Type = "BookingPaid",
                RelatedId = bookingId,
                Icon = "💰",
                ActionUrl = "/PartyClap/Vendor/Dashboard?section=bookings"
            });

            amountPaid = booking.AdvancePaid;
            return true;
        }

        private void EnrichCartItems(List<CartItem> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                var endDate = item.EventEndDate ?? item.EventDate;
                if (item.EventDate.HasValue && endDate.HasValue)
                {
                    item.EventDayCount = _pricingService.CountEventDays(item.EventDate.Value, endDate.Value);
                    item.LineTotal = _pricingService.CalculateCartItemTotal(item);
                }
                else
                {
                    item.EventDayCount = 1;
                    item.LineTotal = item.Cost;
                }
            }
        }

        private (string customerId, string cookieId) GetCartContext()
        {
            var customerId = User.IsInRole("Customer") ? HttpContext.GetUserId() : null;
            var cookieId = GetOrCreateCookieId();
            return (customerId, cookieId);
        }

        private void MergeGuestCartIfNeeded(string customerId, string cookieId)
        {
            if (!string.IsNullOrWhiteSpace(customerId) && !string.IsNullOrWhiteSpace(cookieId))
            {
                _dataService.MergeGuestCart(customerId, cookieId);
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

        private void PopulateMessagingViewBag(string userId, string userRole, string chatWith)
        {
            var conversations = _dataService.GetUserConversations(userId);
            foreach (var conversation in conversations)
            {
                conversation.OtherUserName = _dataService.GetVendor(conversation.OtherUserId)?.Name ?? "Vendor";
            }

            ViewBag.Conversations = conversations;
            ViewBag.CurrentUserId = userId;
            ViewBag.ChatPartnerId = chatWith;
            ViewBag.MessagingDashboardController = "Customer";
            ViewBag.MessagingDashboardSection = "Messages";

            if (!string.IsNullOrWhiteSpace(chatWith))
            {
                ViewBag.ChatHistory = _dataService.GetChatHistory(userId, chatWith);
                ViewBag.ChatPartnerName = _dataService.GetVendor(chatWith)?.Name ?? "Vendor";
            }
            else
            {
                ViewBag.ChatHistory = new List<Message>();
            }
        }
    }
}
