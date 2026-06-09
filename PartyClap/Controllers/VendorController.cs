using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyClap.Models;
using PartyClap.Services;
using PartyClap.DAL;
using System;
using System.Linq;

namespace PartyClap.Controllers
{
    [Authorize(Roles = "Vendor")]
    public class VendorController : Controller
    {
        private readonly IDataService _dataService;
        private readonly INotificationService _notificationService;
        private readonly LocationDAL _locationDAL;
        private readonly VendorDAL _vendorDAL;
        private readonly IPricingService _pricingService;
        private readonly IOtpService _otpService;
        private readonly ILogger<VendorController> _logger;

        public VendorController(
            IDataService dataService,
            INotificationService notificationService,
            LocationDAL locationDAL,
            VendorDAL vendorDAL,
            IPricingService pricingService,
            IOtpService otpService,
            ILogger<VendorController> logger)
        {
            _dataService = dataService;
            _notificationService = notificationService;
            _locationDAL = locationDAL;
            _vendorDAL = vendorDAL;
            _pricingService = pricingService;
            _otpService = otpService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            ViewBag.Locations = _dataService.GetLocations();
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetStates()
        {
            // Only expose states the admin has enabled (serviceable areas).
            var states = _dataService.GetEnabledStateNames();
            return Json(states);
        }

        [HttpGet]
        [AllowAnonymous]
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
                _logger.LogError(ex, "Error loading cities for state {State}", state);
                return Json(new List<string>());
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetPinCodes(string city, string state = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return Json(new List<Location>());
                }
                var pinCodes = _locationDAL.GetPinCodesByCity(city, state);
                return Json(pinCodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading PIN codes for city {City}", city);
                return Json(new List<Location>());
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SearchPinCodes(string searchTerm)
        {
            var locations = _locationDAL.SearchPinCodes(searchTerm);
            return Json(locations);
        }

        [AllowAnonymous]
        public IActionResult RegistrationSuccess(string id)
        {
            ViewData["VendorId"] = id;
            return View();
        }

        /// <summary>
        /// Saves verified contact details (name, email, phone) after OTP verification.
        /// Creates a draft vendor row keyed by mobile number for the registration wizard.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveVerifiedContact(string firstName, string lastName, string email, string mobile)
        {
            firstName = firstName?.Trim();
            lastName = lastName?.Trim();
            email = EmailRules.Normalize(email);

            if (string.IsNullOrWhiteSpace(firstName))
            {
                return Json(new { success = false, message = "First name is required." });
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                return Json(new { success = false, message = "Last name is required." });
            }

            var fullName = $"{firstName} {lastName}".Trim();
            var nameError = VendorRegistrationRules.ValidateName(fullName);
            if (nameError != null)
            {
                return Json(new { success = false, message = nameError });
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Email is required." });
            }

            var phoneError = PhoneRules.ValidateIndianMobile(mobile, out var normalizedPhone);
            if (phoneError != null)
            {
                return Json(new { success = false, message = phoneError });
            }

            mobile = normalizedPhone;

            if (!_otpService.IsPhoneVerified(mobile))
            {
                return Json(new { success = false, message = "Please verify your mobile number with OTP first." });
            }

            var existingVendor = _dataService.GetVendorByPhone(mobile);
            if (existingVendor != null && _vendorDAL.HasVendorServices(existingVendor.Id))
            {
                return Json(new { success = false, message = "A vendor account already exists with this phone number. Please log in instead." });
            }

            var emailError = EmailRules.ValidateVendorEmail(_dataService, email, existingVendor?.Id, mobile);
            if (emailError != null)
            {
                return Json(new { success = false, message = emailError });
            }

            try
            {
                var vendor = _vendorDAL.SaveVendorContactDraft(firstName, lastName, email, mobile);
                return Json(new
                {
                    success = true,
                    message = "Contact details saved.",
                    vendorId = vendor.Id,
                    firstName,
                    lastName,
                    email = vendor.Email,
                    mobile = vendor.Phone,
                    fullName = vendor.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save verified vendor contact for phone ending {Suffix}", mobile.Length >= 4 ? mobile[^4..] : mobile);
                return Json(new { success = false, message = "Could not save your details. Please try again." });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
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

                vendor.Email = EmailRules.Normalize(vendor.Email);
                vendor.Name = vendor.Name?.Trim();
                vendor.Address = vendor.Address?.Trim();

                var lastName = Request.Form["LastName"].ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(lastName))
                {
                    vendor.Name = string.IsNullOrWhiteSpace(vendor.Name)
                        ? lastName
                        : $"{vendor.Name} {lastName}".Trim();
                }

                var nameError = VendorRegistrationRules.ValidateName(vendor.Name);
                if (nameError != null)
                {
                    ModelState.AddModelError(nameof(Vendor.Name), nameError);
                }

                var phoneError = PhoneRules.ValidateIndianMobile(vendor.Phone, out var normalizedPhone);
                if (phoneError != null)
                {
                    ModelState.AddModelError(nameof(Vendor.Phone), phoneError);
                }
                else
                {
                    vendor.Phone = normalizedPhone;
                }

                var pinError = VendorRegistrationRules.ValidatePinCode(vendor.PinCode, out var normalizedPin);
                if (pinError != null)
                {
                    ModelState.AddModelError(nameof(Vendor.PinCode), pinError);
                }
                else
                {
                    vendor.PinCode = normalizedPin;
                }

                var addressError = VendorRegistrationRules.ValidateAddress(vendor.Address);
                if (addressError != null)
                {
                    ModelState.AddModelError(nameof(Vendor.Address), addressError);
                }

                if (!TryValidateModel(vendor))
                {
                    ViewBag.Locations = _dataService.GetLocations();
                    return View(vendor);
                }

                var emailError = EmailRules.ValidateVendorEmail(_dataService, vendor.Email, null, vendor.Phone);
                if (emailError != null)
                {
                    ModelState.AddModelError(nameof(Vendor.Email), emailError);
                    ViewBag.Locations = _dataService.GetLocations();
                    return View(vendor);
                }

                if (!_otpService.IsPhoneVerified(vendor.Phone))
                {
                    ModelState.AddModelError(nameof(Vendor.Phone), "Please verify your mobile number before completing registration.");
                    ViewBag.Locations = _dataService.GetLocations();
                    return View(vendor);
                }

                var existingVendor = _dataService.GetVendorByPhone(vendor.Phone);
                if (existingVendor != null && _vendorDAL.HasVendorServices(existingVendor.Id))
                {
                    ModelState.AddModelError(nameof(Vendor.Phone), "A vendor account already exists with this phone number. Please log in instead.");
                    ViewBag.Locations = _dataService.GetLocations();
                    return View(vendor);
                }

                var vendorIdFromForm = Request.Form["VendorId"].ToString()?.Trim();
                if (!string.IsNullOrEmpty(vendorIdFromForm))
                {
                    if (existingVendor == null || !string.Equals(existingVendor.Id, vendorIdFromForm, StringComparison.Ordinal))
                    {
                        ModelState.AddModelError(nameof(Vendor.Phone), "Contact verification does not match this registration. Please verify your mobile number again.");
                        ViewBag.Locations = _dataService.GetLocations();
                        return View(vendor);
                    }

                    vendor.Id = existingVendor.Id;
                    vendor.WalletBalance = existingVendor.WalletBalance;
                    vendor.TrustScore = existingVendor.TrustScore;
                }
                else if (existingVendor != null)
                {
                    vendor.Id = existingVendor.Id;
                    vendor.WalletBalance = existingVendor.WalletBalance;
                    vendor.TrustScore = existingVendor.TrustScore;
                }
                else
                {
                    vendor.Id = Guid.NewGuid().ToString();
                    vendor.TrustScore = 100;
                    vendor.WalletBalance = 0;
                }

                vendor.IsRegistered = true;

                // Bank/payment details are collected only after login (vendor dashboard profile).
                vendor.AccountHolderName = null;
                vendor.AccountNumber = null;
                vendor.IfscCode = null;
                vendor.UpiId = null;
                
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
                
                if (existingVendor != null)
                {
                    _dataService.UpdateVendor(vendor);
                }
                else
                {
                    _dataService.AddVendor(vendor);
                }

                VendorExploreHelper.EnsureVendorListed(_dataService, vendor);
                
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

                    var costError = ServicePricingRules.ValidateCost(cost);
                    if (costError != null)
                    {
                        TempData["WarningMessage"] = costError + " Add a valid price under My Services after registration.";
                    }
                    else
                    {
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
                
                // Sign the new vendor in (claims + session) for auto-login.
                await HttpContext.SignInUserAsync("Vendor", vendor.Id, vendor.Name);
                
                // Redirect to Dashboard with success message
                TempData["SuccessMessage"] = "Registration successful! Welcome to PartyClap.";
                // Redirect to Success Page
                return RedirectToAction("RegistrationSuccess", new { id = vendor.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vendor registration failed for email {Email}", vendor?.Email);
                TempData["ErrorMessage"] = "Registration failed. Please try again.";
                ViewBag.Locations = _dataService.GetLocations();
                return View(vendor);
            }
        }

        public IActionResult Dashboard(string id, string section = "overview", string chatWith = null)
        {
            // Block cross-role access even if session/cookie state is inconsistent.
            if (!User.IsInRole("Vendor"))
            {
                if (User.IsInRole("Customer"))
                {
                    TempData["ErrorMessage"] = "Vendor dashboard is only available to vendor accounts.";
                    return RedirectToAction("Dashboard", "Customer");
                }
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Login", "Account");
            }

            // A vendor may only view their own dashboard. Ignore any supplied id
            // and always scope to the authenticated vendor to prevent IDOR.
            id = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(id)) return RedirectToAction("Login", "Account");

            var vendor = _dataService.GetVendor(id);
            if (vendor == null) return NotFound();
            
            // Get all data for different sections
            var bookingsWithDetails = _vendorDAL.GetVendorBookingsWithCustomerDetails(id);
            var serviceRequests = _dataService.GetVendorServiceRequestsWithDetails(id);
            var services = _dataService.GetVendorServices(id);
            var portfolio = _dataService.GetVendorPortfolio(id);
            var walletTransactions = _dataService.GetWalletTransactions(id, "Vendor");

            PopulateMessagingViewBag(id, chatWith);
            
            var scheduleFrom = DateTime.Today;
            var scheduleTo = DateTime.Today.AddDays(60);
            var vendorSchedule = _vendorDAL.GetVendorSchedule(id, scheduleFrom, scheduleTo);

            ViewBag.BookingsWithDetails = bookingsWithDetails;
            ViewBag.ServiceRequests = serviceRequests;
            ViewBag.Services = services;
            ViewBag.Portfolio = portfolio;
            ViewBag.WalletTransactions = walletTransactions;
            ViewBag.Section = section;
            ViewBag.VendorSchedule = vendorSchedule;
            ViewBag.VendorBookedDates = vendorSchedule.Where(s => s.IsBooked).OrderBy(s => s.Date).ToList();
            ViewBag.VendorAvailableDates = vendorSchedule.Where(s => !s.IsBooked && !s.IsUnderProcess).OrderBy(s => s.Date).ToList();
            ViewBag.VendorCalendarBlocks = _dataService.GetVendorCalendarBlocks(id, scheduleFrom, scheduleTo)
                .Where(b => !b.IsAvailable).ToList();
            
            // Calculate Incentive Tier based on approved bookings
            int approvedBookingsCount = 0;
            if (bookingsWithDetails != null)
            {
                approvedBookingsCount = bookingsWithDetails.Count(b => b["Status"].ToString() == "Approved");
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

        [HttpPost]
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public IActionResult RequestPayout(decimal amount)
        {
            var vendorId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");

            var vendor = _dataService.GetVendor(vendorId);
            if (vendor == null) return NotFound();

            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Please enter a valid payout amount greater than ₹0.";
            }
            else if (vendor.WalletBalance <= 0)
            {
                TempData["ErrorMessage"] = "You have no available balance to withdraw.";
            }
            else if (amount > vendor.WalletBalance)
            {
                TempData["ErrorMessage"] = $"Payout amount (₹{amount:N0}) exceeds your available balance of ₹{vendor.WalletBalance:N0}.";
            }
            else
            {
                try
                {
                    var success = _dataService.RequestVendorPayout(vendorId, amount, $"Payout request of ₹{amount:N0}");
                    if (success)
                    {
                        TempData["SuccessMessage"] = $"Payout request for ₹{amount:N0} submitted successfully. It will be processed to your registered account.";
                    }
                    else
                    {
                        // Balance changed between the page load and submit (e.g. concurrent payout).
                        TempData["ErrorMessage"] = "Payout could not be processed due to insufficient balance. Please refresh and try again.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RequestPayout failed for vendor {VendorId}", vendorId);
                    TempData["ErrorMessage"] = "An error occurred while processing your payout. Please try again.";
                }
            }

            return RedirectToAction("Dashboard", new { section = "wallet" });
        }

        [HttpGet]
        [Authorize(Roles = "Vendor")]
        public IActionResult AddService(string vendorId)
        {
            return View(new ServiceListing { VendorId = HttpContext.GetUserId() });
        }

        [HttpPost]
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(ServiceListing service, List<IFormFile> PhotoFiles, List<IFormFile> VideoFiles, List<IFormFile> AudioFiles)
        {
            // Always bind the service to the authenticated vendor (never trust the form value).
            service.VendorId = HttpContext.GetUserId();

            var costError = ServicePricingRules.ValidateCost(service.Cost);
            if (costError != null)
            {
                ModelState.AddModelError(nameof(ServiceListing.Cost), costError);
            }
            var weekendError = ServicePricingRules.ValidateWeekendCost(service.WeekendCost);
            if (weekendError != null)
            {
                ModelState.AddModelError(nameof(ServiceListing.WeekendCost), weekendError);
            }
            if (!TryValidateModel(service))
            {
                return View(service);
            }

            var vendor = _dataService.GetVendor(service.VendorId);
            if (vendor != null)
            {
                if (string.IsNullOrWhiteSpace(vendor.PinCode))
                {
                    TempData["ErrorMessage"] = "Add your 6-digit pin code under Profile → Address before listing services. Customers cannot find you on Explore without a location.";
                    return RedirectToAction("Dashboard", new { section = "profile" });
                }

                if (!vendor.IsRegistered)
                {
                    vendor.IsRegistered = true;
                    vendor.Address = string.IsNullOrWhiteSpace(vendor.Address) ? "Address pending" : vendor.Address;
                    _dataService.UpdateVendor(vendor);
                }

                VendorExploreHelper.EnsureVendorListed(_dataService, vendor);

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

                return RedirectToAction("Dashboard", new { id = service.VendorId, section = "services" });
            }
            return View(service);
        }

        [HttpGet]
        [Authorize(Roles = "Vendor")]
        public IActionResult EditService(string id)
        {
            var vendorId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");

            var service = _dataService.GetService(id);
            if (service == null || service.VendorId != vendorId)
            {
                return NotFound();
            }

            return View(service);
        }

        [HttpPost]
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(ServiceListing service, IFormFile MediaFile)
        {
             var vendorId = HttpContext.GetUserId();
             if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");
            
            // Verify ownership
            var existingService = _dataService.GetService(service.Id);
            if (existingService == null || existingService.VendorId != vendorId)
            {
                return NotFound();
            }

            // Keep existing data that might not be in form
            service.VendorId = vendorId;
            if (string.IsNullOrEmpty(service.MediaUrl))
            {
                service.MediaUrl = existingService.MediaUrl;
            }

            var costError = ServicePricingRules.ValidateCost(service.Cost);
            if (costError != null)
            {
                ModelState.AddModelError(nameof(ServiceListing.Cost), costError);
            }
            var weekendError = ServicePricingRules.ValidateWeekendCost(service.WeekendCost);
            if (weekendError != null)
            {
                ModelState.AddModelError(nameof(ServiceListing.WeekendCost), weekendError);
            }
            if (!TryValidateModel(service))
            {
                return View(service);
            }

            if (MediaFile != null && MediaFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                    
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + MediaFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await MediaFile.CopyToAsync(fileStream);
                }
                
                service.MediaUrl = "/uploads/" + uniqueFileName;
            }

            _dataService.UpdateService(service);
            TempData["SuccessMessage"] = "Service updated successfully!";
            return RedirectToAction("Dashboard", new { id = vendorId, section = "services" });
        }

        [HttpPost]
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteService(string id)
        {
            var vendorId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");

            var service = _dataService.GetService(id);
            if (service != null && service.VendorId == vendorId)
            {
                _dataService.DeleteService(id);
                TempData["SuccessMessage"] = "Service deleted successfully!";
            }

            return RedirectToAction("Dashboard", new { id = vendorId, section = "services" });
        }

        [HttpGet]
        [Authorize(Roles = "Vendor")]
        public IActionResult Profile()
        {
            var vendorId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");
            
            var vendor = _dataService.GetVendor(vendorId);
            return View(vendor);
        }

        [HttpPost]
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(Vendor vendor)
        {
            // Bind the update to the authenticated vendor, never a posted Id.
            vendor.Id = HttpContext.GetUserId();
            var existingVendor = _dataService.GetVendor(vendor.Id);
            if (existingVendor == null) return NotFound();

            // Disabled profile fields are not posted from the dashboard form — preserve them.
            if (string.IsNullOrWhiteSpace(vendor.Email))
            {
                vendor.Email = existingVendor.Email;
            }
            else
            {
                vendor.Email = EmailRules.Normalize(vendor.Email);
            }
            if (string.IsNullOrWhiteSpace(vendor.Phone))
            {
                vendor.Phone = existingVendor.Phone;
            }

            var emailError = EmailRules.ValidateVendorEmail(_dataService, vendor.Email, vendor.Id);
            if (emailError != null)
            {
                ModelState.AddModelError(nameof(Vendor.Email), emailError);
            }

            var payoutError = BankDetailsRules.ValidatePayoutDetails(
                vendor.AccountHolderName,
                vendor.AccountNumber,
                vendor.IfscCode,
                vendor.UpiId);
            if (payoutError != null)
            {
                ModelState.AddModelError(nameof(Vendor.AccountNumber), payoutError);
            }

            if (ModelState.IsValid)
            {
                    existingVendor.Name = vendor.Name;
                    existingVendor.Email = vendor.Email;
                    existingVendor.Phone = vendor.Phone;
                    existingVendor.Address = vendor.Address;
                    existingVendor.PinCode = vendor.PinCode;
                    existingVendor.ProfilePicture = vendor.ProfilePicture;

                    if (!string.IsNullOrWhiteSpace(existingVendor.PinCode) && _vendorDAL.HasVendorServices(existingVendor.Id))
                    {
                        existingVendor.IsRegistered = true;
                    }

                    VendorExploreHelper.EnsureVendorListed(_dataService, existingVendor);
                    
                    existingVendor.AccountHolderName = vendor.AccountHolderName?.Trim();
                    existingVendor.AccountNumber = new string((vendor.AccountNumber ?? string.Empty).Where(char.IsDigit).ToArray());
                    existingVendor.IfscCode = BankDetailsRules.NormalizeIfscCode(vendor.IfscCode);
                    existingVendor.UpiId = vendor.UpiId?.Trim();
                    
                    _dataService.UpdateVendor(existingVendor);
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Dashboard", new { section = "profile" });
            }

            TempData["ErrorMessage"] = payoutError ?? emailError ?? "Please correct the errors below and try again.";
            return RedirectToAction("Dashboard", new { section = "profile" });
        }
        [Authorize(Roles = "Vendor")]
        public IActionResult Portfolio()
        {
            var vendorId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");
            
            var portfolio = _dataService.GetVendorPortfolio(vendorId);
            ViewBag.VendorId = vendorId;
            return View(portfolio);
        }

        [HttpPost]
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPortfolioItem(PortfolioItem item)
        {
            // Always bind portfolio uploads to the authenticated vendor.
            item.VendorId = HttpContext.GetUserId();
            
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
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateBookingStatus(string bookingId, string status, decimal? adjustedCost)
        {
            var vendorId = HttpContext.GetUserId();

            // Ownership check: a vendor may only modify their own bookings.
            var ownedBooking = _dataService.GetBooking(bookingId);
            if (ownedBooking == null || ownedBooking.VendorId != vendorId)
            {
                return NotFound();
            }

            decimal? vendorCost = null;
            decimal? customerTotal = null;
            
            if (adjustedCost.HasValue && status == "Approved")
            {
                vendorCost = adjustedCost.Value;
                customerTotal = _pricingService.CalculatePricing(adjustedCost.Value).CustomerTotalCost;
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
                        Title = "Booking Approved ✅",
                        Message = $"{vendor?.Name ?? "Vendor"} accepted your booking for {service?.ServiceType ?? "service"} on {existingBooking.EventDate:MMM dd, yyyy}. Tap to pay the full amount and confirm.",
                        Type = "BookingApproved",
                        RelatedId = bookingId,
                        Icon = "✅",
                        ActionUrl = "/PartyClap/Customer/ViewCart"
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
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateServiceRequestStatus(string requestId, string status)
        {
            try
            {
                if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(status))
                {
                    TempData["ErrorMessage"] = "Invalid request parameters.";
                    return RedirectToAction("Dashboard");
                }

                var vendorId = HttpContext.GetUserId();
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

                _dataService.UpdateServiceRequestStatus(requestId, status);

                var vendor = _dataService.GetVendor(vendorId);
                var service = _dataService.GetService(request.ServiceId);

                if (status == "Approved")
                {
                    _notificationService.CreateNotification(new Notification
                    {
                        UserId = request.CustomerId,
                        UserType = "Customer",
                        Title = "Request Approved",
                        Message = $"{vendor?.Name ?? "Vendor"} approved your {service?.ServiceType ?? "service"} request for {request.EventDate:MMM dd, yyyy}. Open your Accepted cart to pay and confirm.",
                        Type = "ServiceRequestApproved",
                        RelatedId = requestId,
                        Icon = "✅",
                        ActionUrl = "/PartyClap/Customer/ViewCart"
                    });
                    TempData["SuccessMessage"] = "Service request approved! The customer can pay now.";
                }
                else if (status == "Rejected")
                {
                    _notificationService.CreateNotification(new Notification
                    {
                        UserId = request.CustomerId,
                        UserType = "Customer",
                        Title = "Request Declined",
                        Message = $"{vendor?.Name ?? "Vendor"} cannot take your {service?.ServiceType ?? "service"} request on {request.EventDate:MMM dd, yyyy}.",
                        Type = "ServiceRequestRejected",
                        RelatedId = requestId,
                        Icon = "❌",
                        ActionUrl = "/PartyClap/Customer/ViewCart"
                    });
                    TempData["InfoMessage"] = "Service request rejected. The customer has been notified.";
                }

                return RedirectToAction("Dashboard", new { section = "servicerequests" });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error updating service request. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }
        [HttpPost]
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public IActionResult SaveCalendarBlock(DateTime blockDate, string startTime, string endTime, bool isAvailable, string label, int? blockId = null, bool isHourly = false)
        {
            var vendorId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(vendorId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (blockDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Cannot set availability for past dates.";
                return RedirectToAction("Dashboard", new { section = "availability" });
            }

            if (isAvailable)
            {
                TempData["ErrorMessage"] = "Set hours is no longer supported. Use Block time to mark unavailable slots.";
                return RedirectToAction("Dashboard", new { section = "availability" });
            }

            var block = new VendorCalendarBlock
            {
                VendorId = vendorId,
                BlockDate = blockDate.Date,
                StartTime = string.IsNullOrWhiteSpace(startTime) ? null : startTime.Trim(),
                EndTime = string.IsNullOrWhiteSpace(endTime) ? null : endTime.Trim(),
                IsAvailable = isAvailable,
                IsHourly = isHourly,
                Label = string.IsNullOrWhiteSpace(label) ? (isAvailable ? "Available" : "PartyClap") : label.Trim(),
                CreatedDate = DateTime.Now
            };

            if (blockId.HasValue && blockId.Value > 0)
            {
                var existing = _dataService.GetVendorCalendarBlock(blockId.Value, vendorId);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Calendar entry not found.";
                    return RedirectToAction("Dashboard", new { section = "availability" });
                }

                block.Id = existing.Id;
                block.CreatedDate = existing.CreatedDate;
                _dataService.UpdateVendorCalendarBlock(block);
                TempData["SuccessMessage"] = isAvailable
                    ? "Available slot updated and synced to Explore."
                    : "Blocked slot updated and synced to Explore.";
            }
            else
            {
                _dataService.AddVendorCalendarBlock(block);
                TempData["SuccessMessage"] = isAvailable
                    ? "Available slot saved and synced to Explore."
                    : "Time block saved and synced to Explore.";
            }

            return RedirectToAction("Dashboard", new { section = "availability" });
        }

        [HttpPost]
        [Authorize(Roles = "Vendor")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCalendarBlock(int blockId)
        {
            var vendorId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(vendorId))
            {
                return RedirectToAction("Login", "Account");
            }

            _dataService.DeleteVendorCalendarBlock(blockId, vendorId);
            TempData["SuccessMessage"] = "Calendar entry removed.";
            return RedirectToAction("Dashboard", new { section = "availability" });
        }

        [HttpGet]
        [Authorize(Roles = "Vendor")]
        public IActionResult Insurance()
        {
            var vendorId = HttpContext.GetUserId();
            if (string.IsNullOrEmpty(vendorId)) return RedirectToAction("Login", "Account");
            
            var vendor = _dataService.GetVendor(vendorId);
            return View(vendor);
        }

        private void PopulateMessagingViewBag(string userId, string chatWith)
        {
            var conversations = _dataService.GetUserConversations(userId);
            foreach (var conversation in conversations)
            {
                conversation.OtherUserName = _dataService.GetCustomerById(conversation.OtherUserId)?.Name ?? "Customer";
            }

            ViewBag.Conversations = conversations;
            ViewBag.CurrentUserId = userId;
            ViewBag.ChatPartnerId = chatWith;
            ViewBag.MessagingDashboardController = "Vendor";
            ViewBag.MessagingDashboardSection = "messages";

            if (!string.IsNullOrWhiteSpace(chatWith))
            {
                ViewBag.ChatHistory = _dataService.GetChatHistory(userId, chatWith);
                ViewBag.ChatPartnerName = _dataService.GetCustomerById(chatWith)?.Name ?? "Customer";
            }
            else
            {
                ViewBag.ChatHistory = new List<Message>();
            }
        }
    }
}
