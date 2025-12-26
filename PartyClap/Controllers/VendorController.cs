using Microsoft.AspNetCore.Mvc;
using PartyClap.Models;
using PartyClap.Services;
using PartyClap.DAL;
using System;
using System.Linq;

namespace PartyClap.Controllers
{
    public class VendorController : Controller
    {
        private readonly IDataService _dataService;
        private readonly INotificationService _notificationService;
        private readonly LocationDAL _locationDAL;
        private readonly VendorDAL _vendorDAL;

        public VendorController(IDataService dataService, INotificationService notificationService, LocationDAL locationDAL, VendorDAL vendorDAL)
        {
            _dataService = dataService;
            _notificationService = notificationService;
            _locationDAL = locationDAL;
            _vendorDAL = vendorDAL;
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Locations = _dataService.GetLocations();
            return View();
        }

        [HttpGet]
        public IActionResult GetStates()
        {
            var states = _locationDAL.GetStates();
            return Json(states);
        }

        [HttpGet]
        public IActionResult GetCities(string state)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(state))
                {
                    return Json(new List<string>());
                }
                var cities = _locationDAL.GetCitiesByState(state);
                return Json(cities);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCities: {ex.Message}");
                return Json(new List<string>());
            }
        }

        [HttpGet]
        public IActionResult GetPinCodes(string city)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return Json(new List<Location>());
                }
                var pinCodes = _locationDAL.GetPinCodesByCity(city);
                return Json(pinCodes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetPinCodes: {ex.Message}");
                return Json(new List<Location>());
            }
        }

        [HttpGet]
        public IActionResult SearchPinCodes(string searchTerm)
        {
            var locations = _locationDAL.SearchPinCodes(searchTerm);
            return Json(locations);
        }

        public IActionResult RegistrationSuccess(string id)
        {
            ViewData["VendorId"] = id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Vendor vendor, List<string> ServiceLocations, List<IFormFile> PhotoFiles, List<IFormFile> VideoFiles)
        {
            try
            {
                // Debug: Log what we received
                System.Diagnostics.Debug.WriteLine("=== Vendor Registration Debug ===");
                System.Diagnostics.Debug.WriteLine($"ServiceLocations parameter: {(ServiceLocations == null ? "NULL" : $"Count: {ServiceLocations.Count}")}");
                
                // Read ServiceLocations directly from Request.Form (more reliable than model binding)
                var serviceLocationsFromForm = new List<string>();
                
                // Method 1: Try indexed format ServiceLocations[0], ServiceLocations[1], etc.
                var index = 0;
                while (true)
                {
                    var value = Request.Form[$"ServiceLocations[{index}]"].ToString();
                    if (string.IsNullOrEmpty(value))
                        break;
                    serviceLocationsFromForm.Add(value);
                    System.Diagnostics.Debug.WriteLine($"ServiceLocations[{index}] from form: {value}");
                    index++;
                }
                
                // Method 2: Try without brackets if indexed didn't work
                if (serviceLocationsFromForm.Count == 0)
                {
                    var allFormKeys = Request.Form.Keys.Where(k => k.Contains("ServiceLocation", StringComparison.OrdinalIgnoreCase));
                    foreach (var key in allFormKeys)
                    {
                        var value = Request.Form[key].ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            serviceLocationsFromForm.Add(value);
                            System.Diagnostics.Debug.WriteLine($"ServiceLocation from key '{key}': {value}");
                        }
                    }
                }
                
                // Method 3: Try reading all form values to see what's actually being sent
                System.Diagnostics.Debug.WriteLine("=== All form keys ===");
                foreach (var key in Request.Form.Keys)
                {
                    var values = Request.Form[key];
                    if (values.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {key} = {string.Join(", ", values)}");
                    }
                }
                System.Diagnostics.Debug.WriteLine("=== End form keys ===");
                
                // Use form data (prefer Request.Form over model binding for ServiceLocations)
                if (serviceLocationsFromForm.Any())
                {
                    ServiceLocations = serviceLocationsFromForm;
                    System.Diagnostics.Debug.WriteLine($"✓ Using ServiceLocations from Request.Form: Count = {ServiceLocations.Count}");
                }
                else if (ServiceLocations != null && ServiceLocations.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"✓ Using ServiceLocations from model binding: Count = {ServiceLocations.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠ WARNING: No ServiceLocations found in form data or model binding!");
                    // Initialize empty list to prevent null reference
                    ServiceLocations = new List<string>();
                }
                
                // Generate unique ID for vendor
                vendor.Id = Guid.NewGuid().ToString();
                
                // Set registration status
                vendor.IsRegistered = true;
                vendor.TrustScore = 100;
                vendor.WalletBalance = 0;
                
                // Extract bank details from form
                vendor.AccountHolderName = Request.Form["AccountHolderName"].ToString();
                vendor.AccountNumber = Request.Form["AccountNumber"].ToString();
                vendor.IfscCode = Request.Form["IfscCode"].ToString();
                vendor.UpiId = Request.Form["UpiId"].ToString();
                
                // Handle Service Locations - extract pin codes from location strings
                if (ServiceLocations != null && ServiceLocations.Any())
                {
                    var pinCodes = new List<string>();
                    foreach (var location in ServiceLocations)
                    {
                        if (!string.IsNullOrWhiteSpace(location))
                        {
                            // Extract pin code from location string (format: "City, State (PinCode)" or just "PinCode")
                            var pinCodeMatch = System.Text.RegularExpressions.Regex.Match(location, @"\((\d{6})\)");
                            if (pinCodeMatch.Success)
                            {
                                pinCodes.Add(pinCodeMatch.Groups[1].Value);
                            }
                            else if (System.Text.RegularExpressions.Regex.IsMatch(location, @"^\d{6}$"))
                            {
                                // If it's just a pin code
                                pinCodes.Add(location);
                            }
                            else
                            {
                                // Try to extract from the vendor's main pin code if available
                                if (!string.IsNullOrWhiteSpace(vendor.PinCode))
                                {
                                    pinCodes.Add(vendor.PinCode);
                                }
                            }
                        }
                    }
                    vendor.ServiceLocations = pinCodes.Distinct().ToList();
                }
                
                // Add vendor to database
                _dataService.AddVendor(vendor);
                
                // Handle Service from form (if Services[0] data exists in form)
                var serviceType = Request.Form["Services[0].ServiceType"].ToString();
                var serviceCost = Request.Form["Services[0].Cost"].ToString();
                var serviceAttributes = Request.Form["Services[0].Attributes"].ToString();
                
                // Debug: Log what we received
                System.Diagnostics.Debug.WriteLine($"ServiceType: {serviceType}");
                System.Diagnostics.Debug.WriteLine($"ServiceCost: {serviceCost}");
                System.Diagnostics.Debug.WriteLine($"ServiceAttributes: {serviceAttributes}");
                
                // Also try alternative form field names
                if (string.IsNullOrEmpty(serviceType))
                {
                    serviceType = Request.Form["ServiceType"].ToString();
                }
                if (string.IsNullOrEmpty(serviceCost))
                {
                    serviceCost = Request.Form["Cost"].ToString();
                    if (string.IsNullOrEmpty(serviceCost))
                    {
                        serviceCost = Request.Form["BasePrice"].ToString();
                    }
                }
                if (string.IsNullOrEmpty(serviceAttributes))
                {
                    serviceAttributes = Request.Form["Attributes"].ToString();
                }
                
                // Log all form keys for debugging
                System.Diagnostics.Debug.WriteLine("Form Keys: " + string.Join(", ", Request.Form.Keys));
                
                if (!string.IsNullOrEmpty(serviceType))
                {
                    // Parse attributes JSON to get pricing model and dynamic pricing
                    var attributesJson = serviceAttributes;
                    var pricingModel = "event"; // Default
                    decimal? weekendCost = null;
                    
                    if (!string.IsNullOrEmpty(attributesJson))
                    {
                        try
                        {
                            var attributes = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(attributesJson);
                            
                            // Get pricing model
                            if (attributes.TryGetProperty("pricingModel", out var pricingModelElement))
                            {
                                pricingModel = pricingModelElement.GetString() ?? "event";
                            }
                            
                            // Get dynamic pricing weekend cost
                            if (attributes.TryGetProperty("dynamicPricing", out var dynamicPricingElement))
                            {
                                if (dynamicPricingElement.TryGetProperty("weekend", out var weekendElement))
                                {
                                    if (weekendElement.TryGetProperty("base", out var weekendBaseElement))
                                    {
                                        weekendCost = weekendBaseElement.GetDecimal();
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // If JSON parsing fails, use defaults
                        }
                    }
                    
                    // Parse cost
                    decimal cost = 0;
                    if (!string.IsNullOrEmpty(serviceCost) && decimal.TryParse(serviceCost, out var parsedCost))
                    {
                        cost = parsedCost;
                    }
                    
                    var service = new ServiceListing
                    {
                        Id = Guid.NewGuid().ToString(),
                        VendorId = vendor.Id,
                        ServiceType = serviceType,
                        Cost = cost,
                        Unit = pricingModel,
                        Attributes = serviceAttributes,
                        WeekendCost = weekendCost,
                        Description = $"{serviceType} service by {vendor.Name}"
                    };
                    
                    _dataService.AddService(service);
                }
                
                // Handle Portfolio Files Upload
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "portfolio");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                
                // Upload Photos
                if (PhotoFiles != null && PhotoFiles.Any())
                {
                    foreach (var photo in PhotoFiles.Take(10)) // Max 10 photos
                    {
                        if (photo.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await photo.CopyToAsync(fileStream);
                            }
                            
                            var portfolioItem = new PortfolioItem
                            {
                                Id = Guid.NewGuid().ToString(),
                                VendorId = vendor.Id,
                                MediaUrl = "/uploads/portfolio/" + uniqueFileName,
                                MediaType = "Image",
                                Title = "Portfolio Image",
                                Description = "Uploaded during registration"
                            };
                            
                            _dataService.AddPortfolioItem(portfolioItem);
                        }
                    }
                }
                
                // Upload Videos
                if (VideoFiles != null && VideoFiles.Any())
                {
                    foreach (var video in VideoFiles.Take(10)) // Max 10 videos
                    {
                        if (video.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + video.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await video.CopyToAsync(fileStream);
                            }
                            
                            var portfolioItem = new PortfolioItem
                            {
                                Id = Guid.NewGuid().ToString(),
                                VendorId = vendor.Id,
                                MediaUrl = "/uploads/portfolio/" + uniqueFileName,
                                MediaType = "Video",
                                Title = "Portfolio Video",
                                Description = "Uploaded during registration"
                            };
                            
                            _dataService.AddPortfolioItem(portfolioItem);
                        }
                    }
                }
                
                // Set session for auto-login
                HttpContext.Session.SetString("UserId", vendor.Id);
                HttpContext.Session.SetString("UserRole", "Vendor");
                
                // Redirect to Dashboard with success message
                TempData["SuccessMessage"] = "Registration successful! Welcome to PartyClap.";
                // Redirect to Success Page
                return RedirectToAction("RegistrationSuccess", new { id = vendor.Id });
            }
            catch (Exception ex)
            {
                // Log error and show user-friendly message
                TempData["ErrorMessage"] = "Registration failed. Please try again.";
                ViewBag.Locations = _dataService.GetLocations();
                return View(vendor);
            }
        }

        public IActionResult Dashboard(string id)
        {
            // If id is null, try to get from session
            if (string.IsNullOrEmpty(id))
            {
                id = HttpContext.Session.GetString("UserId");
            }

            if (string.IsNullOrEmpty(id)) return RedirectToAction("Login", "Account");

            var vendor = _dataService.GetVendor(id);
            if (vendor == null) return NotFound();
            
            // Get bookings with customer details for this vendor
            ViewBag.BookingsWithDetails = _vendorDAL.GetVendorBookingsWithCustomerDetails(id);
            
            // Get bookings (for backward compatibility)
            ViewBag.Bookings = _dataService.GetVendorBookings(id);
            
            // Get service requests for this vendor
            var serviceRequests = _dataService.GetVendorServiceRequestsWithDetails(id);
            ViewBag.ServiceRequests = serviceRequests;
            
            // Debug: Log service requests count
            System.Diagnostics.Debug.WriteLine($"Vendor {id} has {serviceRequests?.Count ?? 0} service requests");
            
            // Get services for this vendor
            ViewBag.Services = _dataService.GetVendorServices(id);
            
            // Calculate Incentive Tier based on approved bookings
            int approvedBookingsCount = 0;
            if (ViewBag.BookingsWithDetails != null)
            {
                var bookings = ViewBag.BookingsWithDetails as List<Dictionary<string, object>>;
                approvedBookingsCount = bookings.Count(b => b["Status"].ToString() == "Approved");
            }

            // Define Tiers
            string currentTier = "Standard";
            string nextTier = "Silver";
            int bookingsForNextTier = 5;
            int bookingsForCurrentTier = 0;
            string currentcommission = "10%";
            string nextCommission = "8%";

            if (approvedBookingsCount >= 50)
            {
                currentTier = "Platinum";
                nextTier = "Max Level";
                bookingsForCurrentTier = 50;
                bookingsForNextTier = 1000; // Cap
                currentcommission = "4%";
                nextCommission = "4%";
            }
            else if (approvedBookingsCount >= 20)
            {
                currentTier = "Gold";
                nextTier = "Platinum";
                bookingsForCurrentTier = 20;
                bookingsForNextTier = 50;
                currentcommission = "6%";
                nextCommission = "4%";
            }
            else if (approvedBookingsCount >= 5)
            {
                currentTier = "Silver";
                nextTier = "Gold";
                bookingsForCurrentTier = 5;
                bookingsForNextTier = 20;
                currentcommission = "8%";
                nextCommission = "6%";
            }

            int neededForNext = bookingsForNextTier - approvedBookingsCount;
            double progress = nextTier == "Max Level" ? 100 : 
                ((double)(approvedBookingsCount - bookingsForCurrentTier) / (bookingsForNextTier - bookingsForCurrentTier)) * 100;

            ViewBag.IncentiveStats = new {
                Tier = currentTier,
                Count = approvedBookingsCount,
                NextTier = nextTier,
                Needed = neededForNext,
                Progress = progress,
                Commission = currentcommission,
                NextCommission = nextCommission
            };

            return View(vendor);
        }

        [HttpGet]
        public IActionResult AddService(string vendorId)
        {
            return View(new ServiceListing { VendorId = vendorId });
        }

        [HttpPost]
        public async Task<IActionResult> AddService(ServiceListing service, List<IFormFile> PhotoFiles, List<IFormFile> VideoFiles, List<IFormFile> AudioFiles)
        {
            // Ensure VendorId is set
            if (string.IsNullOrEmpty(service.VendorId))
            {
                 service.VendorId = HttpContext.Session.GetString("UserId");
            }

            var vendor = _dataService.GetVendor(service.VendorId);
            if (vendor != null)
            {
                service.Id = Guid.NewGuid().ToString();
                
                // Handle Main Service Image Upload
                if (service.MediaFile != null && service.MediaFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);
                        
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + service.MediaFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await service.MediaFile.CopyToAsync(fileStream);
                    }
                    
                    service.MediaUrl = "/uploads/" + uniqueFileName;
                }
                
                _dataService.AddService(service);

                // Handle Additional Portfolio Files (Photos, Videos, Audio)
                var portfolioUploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "portfolio");
                if (!Directory.Exists(portfolioUploadsFolder))
                    Directory.CreateDirectory(portfolioUploadsFolder);

                // 1. Photos
                if (PhotoFiles != null && PhotoFiles.Any())
                {
                    foreach (var photo in PhotoFiles)
                    {
                        if (photo.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                            var filePath = Path.Combine(portfolioUploadsFolder, uniqueFileName);
                            using (var stream = new FileStream(filePath, FileMode.Create)) await photo.CopyToAsync(stream);

                            _dataService.AddPortfolioItem(new PortfolioItem
                            {
                                Id = Guid.NewGuid().ToString(),
                                VendorId = service.VendorId,
                                MediaType = "Image",
                                MediaUrl = "/uploads/portfolio/" + uniqueFileName,
                                Title = "Service Photo",
                                Description = $"Photo for service: {service.ServiceType}"
                            });
                        }
                    }
                }

                // 2. Videos
                if (VideoFiles != null && VideoFiles.Any())
                {
                    foreach (var video in VideoFiles)
                    {
                        if (video.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + video.FileName;
                            var filePath = Path.Combine(portfolioUploadsFolder, uniqueFileName);
                            using (var stream = new FileStream(filePath, FileMode.Create)) await video.CopyToAsync(stream);

                            _dataService.AddPortfolioItem(new PortfolioItem
                            {
                                Id = Guid.NewGuid().ToString(),
                                VendorId = service.VendorId,
                                MediaType = "Video",
                                MediaUrl = "/uploads/portfolio/" + uniqueFileName,
                                Title = "Service Video",
                                Description = $"Video for service: {service.ServiceType}"
                            });
                        }
                    }
                }

                // 3. Audio
                if (AudioFiles != null && AudioFiles.Any())
                {
                    foreach (var audio in AudioFiles)
                    {
                        if (audio.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + audio.FileName;
                            var filePath = Path.Combine(portfolioUploadsFolder, uniqueFileName);
                            using (var stream = new FileStream(filePath, FileMode.Create)) await audio.CopyToAsync(stream);

                            _dataService.AddPortfolioItem(new PortfolioItem
                            {
                                Id = Guid.NewGuid().ToString(),
                                VendorId = service.VendorId,
                                MediaType = "Audio",
                                MediaUrl = "/uploads/portfolio/" + uniqueFileName,
                                Title = "Service Audio",
                                Description = $"Audio for service: {service.ServiceType}"
                            });
                        }
                    }
                }

                return RedirectToAction("Dashboard", new { id = service.VendorId });
            }
            return View(service);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var vendorId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");
            
            var vendor = _dataService.GetVendor(vendorId);
            return View(vendor);
        }

        [HttpPost]
        public IActionResult Profile(Vendor vendor)
        {
            if (ModelState.IsValid)
            {
                // Ensure critical fields are preserved if not in form
                var existingVendor = _dataService.GetVendor(vendor.Id);
                if (existingVendor != null)
                {
                    existingVendor.Name = vendor.Name;
                    existingVendor.Email = vendor.Email;
                    existingVendor.Phone = vendor.Phone;
                    existingVendor.Address = vendor.Address;
                    existingVendor.PinCode = vendor.PinCode;
                    
                    _dataService.UpdateVendor(existingVendor);
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }
            }
            return View(vendor);
        }
        public IActionResult Portfolio()
        {
            var vendorId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");
            
            var portfolio = _dataService.GetVendorPortfolio(vendorId);
            ViewBag.VendorId = vendorId;
            return View(portfolio);
        }

        [HttpPost]
        public async Task<IActionResult> UploadPortfolioItem(PortfolioItem item)
        {
             if (string.IsNullOrEmpty(item.VendorId))
            {
                 item.VendorId = HttpContext.Session.GetString("UserId");
            }
            
            if (item.File != null && item.File.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                    
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + item.File.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await item.File.CopyToAsync(fileStream);
                }
                
                item.MediaUrl = "/uploads/" + uniqueFileName;
                item.Id = Guid.NewGuid().ToString();
                
                _dataService.AddPortfolioItem(item);
            }
            
            return RedirectToAction("Portfolio");
        }

        [HttpPost]
        public IActionResult UpdateBookingStatus(string bookingId, string status, decimal? adjustedCost)
        {
            decimal? vendorCost = null;
            decimal? customerTotal = null;
            
            if (adjustedCost.HasValue && status == "Approved")
            {
                vendorCost = adjustedCost.Value;
                customerTotal = adjustedCost.Value * 1.10m; // Apply 10% markup
            }

            // Refund logic for Cancellations
            if (status == "Cancelled")
            {
                var booking = _dataService.GetBooking(bookingId);
                if (booking != null)
                {
                    decimal refundAmount = booking.AdvancePaid;
                    if (booking.BalancePaidOnApp)
                    {
                        refundAmount += booking.BalanceAmount;
                    }

                    if (refundAmount > 0)
                    {
                        _dataService.AddMoneyToWallet(booking.CustomerId, refundAmount, $"Refund for Cancelled Booking #{bookingId.Substring(0, 8)}");
                    }
                }
            }
            
            _dataService.UpdateBookingStatus(bookingId, status, vendorCost, customerTotal);
            
            // Create notification for customer
            var existingBooking = _dataService.GetBooking(bookingId);
            if (existingBooking != null)
            {
                var vendor = _dataService.GetVendor(existingBooking.VendorId);
                var service = _dataService.GetService(existingBooking.ServiceId);
                
                if (status == "Approved")
                {
                    var notification = new Notification
                    {
                        UserId = existingBooking.CustomerId,
                        UserType = "Customer",
                        Title = "Booking Confirmed! ✅",
                        Message = $"Great news! {vendor?.Name ?? "Vendor"} has accepted your booking for {service?.ServiceType ?? "service"} on {existingBooking.EventDate:MMM dd, yyyy}. Total: ₹{existingBooking.CustomerTotalCost:N2}",
                        Type = "BookingApproved",
                        RelatedId = bookingId,
                        Icon = "✅",
                        ActionUrl = "/PartyClap/Customer/Dashboard"
                    };
                    _notificationService.CreateNotification(notification);
                }
                else if (status == "Rejected")
                {
                    var notification = new Notification
                    {
                        UserId = existingBooking.CustomerId,
                        UserType = "Customer",
                        Title = "Booking Declined ❌",
                        Message = $"Unfortunately, {vendor?.Name ?? "Vendor"} cannot accept your booking for {service?.ServiceType ?? "service"} on {existingBooking.EventDate:MMM dd, yyyy}. Please explore other vendors.",
                        Type = "BookingRejected",
                        RelatedId = bookingId,
                        Icon = "❌",
                        ActionUrl = "/PartyClap/Customer/Explore"
                    };
                    _notificationService.CreateNotification(notification);
                }
                else if (status == "Cancelled")
                {
                    var notification = new Notification
                    {
                        UserId = existingBooking.CustomerId,
                        UserType = "Customer",
                        Title = "Booking Cancelled ⚠️",
                        Message = $"Your booking with {vendor?.Name ?? "Vendor"} has been cancelled. Any amount paid has been refunded to your wallet.",
                        Type = "BookingCancelled",
                        RelatedId = bookingId,
                        Icon = "⚠️",
                        ActionUrl = "/PartyClap/Customer/Dashboard"
                    };
                    _notificationService.CreateNotification(notification);
                }
            }
            
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult UpdateServiceRequestStatus(string requestId, string status)
        {
            try
            {
                if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(status))
                {
                    TempData["ErrorMessage"] = "Invalid request parameters.";
                    return RedirectToAction("Dashboard");
                }

                var vendorId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(vendorId))
                {
                    TempData["ErrorMessage"] = "Please login to update service requests.";
                    return RedirectToAction("Login", "Account");
                }

                // Verify the request belongs to this vendor
                var requests = _dataService.GetVendorServiceRequests(vendorId);
                var request = requests.FirstOrDefault(r => r.Id == requestId);
                
                if (request == null)
                {
                    TempData["ErrorMessage"] = "Service request not found or you don't have permission to update it.";
                    return RedirectToAction("Dashboard");
                }

                // Update the status
                _dataService.UpdateServiceRequestStatus(requestId, status);

                if (status == "Approved")
                {
                    TempData["SuccessMessage"] = "Service request approved! The customer will be notified.";
                }
                else if (status == "Rejected")
                {
                    TempData["InfoMessage"] = "Service request rejected. The customer has been notified.";
                }

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating service request: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }
        [HttpGet]
        public IActionResult Insurance()
        {
            var vendorId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");
            
            var vendor = _dataService.GetVendor(vendorId);
            return View(vendor);
        }
    }
}
