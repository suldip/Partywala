using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PartyClap.Services;
using PartyClap.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PartyClap.Controllers
{
    public class AccountController : Controller
    {
        private readonly IDataService _dataService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IOtpService _otpService;
        private readonly IOtpSmsSender _otpSmsSender;
        private readonly OtpOptions _otpOptions;

        public AccountController(
            IDataService dataService,
            IPasswordHasher passwordHasher,
            IWebHostEnvironment environment,
            IAuthenticationSchemeProvider schemeProvider,
            IOtpService otpService,
            IOtpSmsSender otpSmsSender,
            Microsoft.Extensions.Options.IOptions<OtpOptions> otpOptions)
        {
            _dataService = dataService;
            _passwordHasher = passwordHasher;
            _environment = environment;
            _schemeProvider = schemeProvider;
            _otpService = otpService;
            _otpSmsSender = otpSmsSender;
            _otpOptions = otpOptions.Value;
        }
        
        [HttpGet]
        public IActionResult Login(string returnUrl = null, string ReturnUrl = null)
        {
            var safeReturnUrl = ReturnUrlValidator.GetSafeLocalUrl(Url, returnUrl ?? ReturnUrl
                ?? TempData["LoginReturnUrl"] as string);
            ViewBag.ReturnUrl = safeReturnUrl;

            if (TempData["LoginError"] is string loginError)
            {
                ViewBag.Error = loginError;
            }

            if (TempData["ShowOtpSection"] is bool showOtp && showOtp)
            {
                ViewBag.ShowOtpSection = true;
                ViewBag.Mobile = TempData["LoginMobile"];
                ViewBag.DebugOtp = TempData["DebugOtp"];
            }

            ViewBag.LoginType = NormalizeLoginType(TempData["LoginType"] as string);

            return View();
        }
        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendOtp(
            string mobile,
            string returnUrl = null,
            bool forLogin = false,
            bool forRegister = false,
            string draftName = null,
            string draftEmail = null,
            string loginType = null)
        {
            var wantsJson = WantsJsonResponse();
            var normalizedLoginType = NormalizeLoginType(loginType);

            try
            {
                if (string.IsNullOrWhiteSpace(mobile))
                {
                    return OtpResponse(wantsJson, false, "Mobile number is required.", mobile, returnUrl, null, forLogin, forRegister, draftName, draftEmail, normalizedLoginType);
                }

                if (!PhoneRules.IsAdminEmail(mobile))
                {
                    var phoneError = PhoneRules.ValidateIndianMobile(mobile, out var normalizedMobile);
                    if (phoneError != null)
                    {
                        return OtpResponse(wantsJson, false, phoneError, mobile, returnUrl, null, forLogin, forRegister, draftName, draftEmail, normalizedLoginType);
                    }

                    mobile = normalizedMobile;

                    if (forLogin && !AccountExistsForLogin(mobile, normalizedLoginType))
                    {
                        return OtpResponse(
                            wantsJson,
                            false,
                            GetMissingAccountMessage(normalizedLoginType),
                            mobile,
                            returnUrl,
                            null,
                            forLogin,
                            forRegister,
                            draftName,
                            draftEmail,
                            normalizedLoginType);
                    }
                }

                if (!HttpContext.Session.IsAvailable)
                {
                    return OtpResponse(
                        wantsJson,
                        false,
                        "Session is unavailable. Please enable cookies and try again.",
                        mobile,
                        returnUrl,
                        null,
                        forLogin,
                        forRegister,
                        draftName,
                        draftEmail,
                        normalizedLoginType);
                }

                await HttpContext.Session.LoadAsync();

                var cooldown = _otpService.GetResendCooldownSecondsRemaining(mobile);
                if (cooldown > 0)
                {
                    return OtpResponse(
                        wantsJson,
                        false,
                        $"Please wait {cooldown} second(s) before requesting another OTP.",
                        mobile,
                        returnUrl,
                        null,
                        forLogin,
                        forRegister,
                        draftName,
                        draftEmail,
                        normalizedLoginType);
                }

                var otp = _otpService.IssueOtp(mobile);
                var smsSent = false;
                if (_otpOptions.SmsEnabled)
                {
                    smsSent = await _otpSmsSender.SendOtpAsync(mobile, otp);
                }

                await HttpContext.Session.CommitAsync();

                var showOnScreen = _otpOptions.ShowOtpOnScreen
                    || (!_otpOptions.SmsEnabled && _otpOptions.ConsoleFallbackOnSmsFailure)
                    || (_otpOptions.SmsEnabled && !smsSent && _otpOptions.ConsoleFallbackOnSmsFailure);

                if (_otpOptions.SmsEnabled && !smsSent && !showOnScreen)
                {
                        return OtpResponse(
                        wantsJson,
                        false,
                        "Could not send OTP via SMS. Please try again later.",
                        mobile,
                        returnUrl,
                        null,
                        forLogin,
                        forRegister,
                        draftName,
                        draftEmail,
                        normalizedLoginType);
                }

                return OtpResponse(
                    wantsJson,
                    true,
                    smsSent ? "OTP sent successfully to your mobile number." : "OTP generated successfully.",
                    mobile,
                    returnUrl,
                    showOnScreen ? otp : null,
                    forLogin,
                    forRegister,
                    draftName,
                    draftEmail,
                    normalizedLoginType);
            }
            catch (Exception)
            {
                return OtpResponse(wantsJson, false, "Could not send OTP. Please try again.", mobile, returnUrl, null, forLogin, forRegister, draftName, draftEmail, normalizedLoginType);
            }
        }

        private bool WantsJsonResponse()
        {
            if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var accept = Request.Headers.Accept.ToString();
            if (accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Login OTP uses a normal HTML form POST and expects a redirect back.
            if (accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // fetch() and other API callers (e.g. registration page) default to */* — return JSON.
            return true;
        }

        private IActionResult OtpResponse(
            bool wantsJson,
            bool success,
            string message,
            string mobile,
            string returnUrl,
            string debugOtp,
            bool forLogin = false,
            bool forRegister = false,
            string draftName = null,
            string draftEmail = null,
            string loginType = "Customer")
        {
            if (wantsJson)
            {
                if (!success)
                {
                    return Json(new { success = false, message });
                }

                if (!string.IsNullOrEmpty(debugOtp))
                {
                    return Json(new { success = true, message, debugOtp });
                }

                return Json(new { success = true, message });
            }

            if (forRegister)
            {
                StashRegisterDraft(draftName, draftEmail);
                TempData["RegisterPhone"] = mobile;
                if (!success)
                {
                    TempData["RegisterOtpError"] = message;
                }
                else
                {
                    TempData["RegisterOtpSent"] = true;
                    if (!string.IsNullOrEmpty(debugOtp))
                    {
                        TempData["RegisterDebugOtp"] = debugOtp;
                    }
                }

                return RedirectToAction("Register", "Customer");
            }

            if (!success)
            {
                TempData["LoginError"] = message;
            }
            else
            {
                TempData["ShowOtpSection"] = true;
                if (!string.IsNullOrEmpty(debugOtp))
                {
                    TempData["DebugOtp"] = debugOtp;
                }
            }

            TempData["LoginMobile"] = mobile;
            TempData["LoginType"] = NormalizeLoginType(loginType);
            TempData["LoginReturnUrl"] = ReturnUrlValidator.GetSafeLocalUrl(Url, returnUrl);
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string mobile, string otp, string loginType = "Customer", string returnUrl = null)
        {
            loginType = NormalizeLoginType(loginType);
            ViewBag.Mobile = mobile;
            ViewBag.LoginType = loginType;
            var safeReturnUrl = ReturnUrlValidator.GetSafeLocalUrl(Url, returnUrl);
            ViewBag.ReturnUrl = safeReturnUrl;

            if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(otp))
            {
                ViewBag.Error = "Please provide your credentials.";
                return View();
            }

            await HttpContext.Session.LoadAsync();

            if (loginType == "Admin")
            {
                if (!PhoneRules.IsAdminEmail(mobile))
                {
                    ViewBag.Error = "Please enter a valid admin email address.";
                    return View();
                }

                var admin = _dataService.GetAdminByEmail(mobile);
                if (admin != null && _passwordHasher.Verify(otp, admin.PasswordHash, out bool needsRehash))
                {
                    if (needsRehash)
                    {
                        _dataService.UpdateAdminPasswordHash(admin.Id, _passwordHasher.Hash(otp));
                    }

                    await HttpContext.SignInUserAsync("Admin", admin.Id, "Admin");
                    return RedirectAfterLogin("Admin", safeReturnUrl);
                }

                ViewBag.Error = "Invalid admin credentials.";
                return View();
            }

            var phoneError = PhoneRules.ValidateIndianMobile(mobile, out var normalizedMobile);
            if (phoneError != null)
            {
                ViewBag.Error = phoneError;
                return View();
            }

            mobile = normalizedMobile;

            if (!_otpService.ValidateOtp(mobile, otp))
            {
                ViewBag.Error = "Invalid or expired OTP. Request a new code and try again.";
                ViewBag.ShowOtpSection = true;
                return View();
            }

            if (loginType == "Vendor")
            {
                var vendor = _dataService.GetVendorByPhone(mobile);
                if (vendor != null)
                {
                    await HttpContext.SignInUserAsync("Vendor", vendor.Id, vendor.Name);
                    return RedirectAfterLogin("Vendor", safeReturnUrl);
                }

                ViewBag.Error = GetMissingAccountMessage("Vendor");
                ViewBag.ShowOtpSection = true;
                return View();
            }

            var customer = _dataService.GetCustomerByPhone(mobile);
            if (customer != null)
            {
                await HttpContext.SignInUserAsync("Customer", customer.Id, customer.Name);
                MergeGuestCartForCustomer(customer.Id);
                return RedirectAfterLogin("Customer", safeReturnUrl);
            }

            ViewBag.Error = GetMissingAccountMessage("Customer");
            ViewBag.ShowRegisterPrompt = loginType == "Customer";
            ViewBag.ShowOtpSection = true;
            return View();
        }

        private static string NormalizeLoginType(string loginType)
        {
            return loginType switch
            {
                "Vendor" => "Vendor",
                "Admin" => "Admin",
                _ => "Customer"
            };
        }

        private static string GetMissingAccountMessage(string loginType)
        {
            return loginType switch
            {
                "Vendor" => "No vendor account found with this mobile number. Please register as a vendor first.",
                "Admin" => "No admin account found with these credentials.",
                _ => "No customer account found with this mobile number. Please create an account first."
            };
        }

        private bool AccountExistsForLogin(string mobile, string loginType)
        {
            return loginType switch
            {
                "Vendor" => _dataService.GetVendorByPhone(mobile) != null,
                "Admin" => false,
                _ => _dataService.GetCustomerByPhone(mobile) != null
            };
        }

        private IActionResult RedirectAfterLogin(string role, string safeReturnUrl)
        {
            if (!string.IsNullOrEmpty(safeReturnUrl))
            {
                return Redirect(safeReturnUrl);
            }

            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Vendor" => RedirectToAction("Dashboard", "Vendor"),
                _ => RedirectToAction("Dashboard", "Customer")
            };
        }

        private void MergeGuestCartForCustomer(string customerId)
        {
            const string cookieName = "PartyClapCartId";
            if (Request.Cookies.TryGetValue(cookieName, out var cookieId) && !string.IsNullOrWhiteSpace(cookieId))
            {
                _dataService.MergeGuestCart(customerId, cookieId);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutUserAsync();
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Called when an authenticated user hits a route their role cannot access
        /// (e.g. a customer opening /Vendor/Dashboard). Redirects to the correct dashboard.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Customer"))
                {
                    TempData["ErrorMessage"] = "You do not have permission to access that area.";
                    return RedirectToAction("Dashboard", "Customer");
                }
                if (User.IsInRole("Vendor"))
                {
                    TempData["ErrorMessage"] = "You do not have permission to access that area.";
                    return RedirectToAction("Dashboard", "Vendor");
                }
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
            }
            return RedirectToAction(nameof(Login));
        }

        // ----------------------------------------------------------------
        // Google (Gmail) login
        // ----------------------------------------------------------------

        /// <summary>
        /// Kicks off the Google OAuth challenge. Falls back gracefully with a
        /// message if Google credentials have not been configured.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string returnUrl = null)
        {
            var googleScheme = await _schemeProvider.GetSchemeAsync(GoogleDefaults.AuthenticationScheme);
            if (googleScheme == null)
            {
                TempData["ErrorMessage"] = "Google sign-in is not configured on this server yet.";
                return RedirectToAction(nameof(Login));
            }

            var safeReturnUrl = ReturnUrlValidator.GetSafeLocalUrl(Url, returnUrl);
            var redirectUrl = Url.Action(nameof(GoogleResponse), "Account", new { returnUrl = safeReturnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles the OAuth callback: reads the verified Google profile, finds or
        /// creates a matching customer, then issues our application auth cookie.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync(AuthConstants.ExternalScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                TempData["ErrorMessage"] = "Google sign-in failed or was cancelled. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name)
                       ?? result.Principal.FindFirstValue(ClaimTypes.GivenName)
                       ?? (email != null ? email.Split('@')[0] : "Google User");

            if (string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync(AuthConstants.ExternalScheme);
                TempData["ErrorMessage"] = "We couldn't read your email from Google. Please try another method.";
                return RedirectToAction(nameof(Login));
            }

            // Find existing customer or provision a new account from the Google profile.
            var customer = _dataService.GetCustomerByEmail(email);
            if (customer == null)
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Email = email,
                    Phone = string.Empty,        // collected later via phone verification
                    Password = string.Empty,     // no local password for Google accounts
                    WalletBalance = 0m
                };
                _dataService.RegisterCustomer(customer);
            }

            await HttpContext.SignInUserAsync("Customer", customer.Id, customer.Name);
            await HttpContext.SignOutAsync(AuthConstants.ExternalScheme);
            MergeGuestCartForCustomer(customer.Id);

            var safeReturnUrl = ReturnUrlValidator.GetSafeLocalUrl(Url, returnUrl);
            if (!string.IsNullOrEmpty(safeReturnUrl))
            {
                return Redirect(safeReturnUrl);
            }
            return RedirectToAction("Explore", "Customer");
        }

        // ----------------------------------------------------------------
        // OTP verification (used by the registration phone-verify step)
        // ----------------------------------------------------------------

        /// <summary>
        /// Verifies a phone OTP against the value stored in session.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult VerifyOtp(
            string mobile,
            string otp,
            bool forRegister = false,
            string draftName = null,
            string draftEmail = null)
        {
            if (string.IsNullOrWhiteSpace(mobile) || string.IsNullOrWhiteSpace(otp))
            {
                return VerifyOtpResponse(forRegister, false, "Mobile number and OTP are required.", mobile, draftName, draftEmail);
            }

            if (!PhoneRules.IsAdminEmail(mobile))
            {
                var phoneError = PhoneRules.ValidateIndianMobile(mobile, out var normalizedMobile);
                if (phoneError != null)
                {
                    return VerifyOtpResponse(forRegister, false, phoneError, mobile, draftName, draftEmail);
                }

                mobile = normalizedMobile;
            }

            if (!_otpService.ValidateOtp(mobile, otp, consume: false))
            {
                return VerifyOtpResponse(forRegister, false, "Invalid or expired OTP.", mobile, draftName, draftEmail, keepOtpRow: true);
            }

            _otpService.MarkPhoneVerified(mobile);
            return VerifyOtpResponse(forRegister, true, "Phone number verified.", mobile, draftName, draftEmail);
        }

        private void StashRegisterDraft(string draftName, string draftEmail)
        {
            if (!string.IsNullOrWhiteSpace(draftName))
            {
                TempData["RegisterDraftName"] = draftName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(draftEmail))
            {
                TempData["RegisterDraftEmail"] = draftEmail.Trim();
            }
        }

        private IActionResult VerifyOtpResponse(
            bool forRegister,
            bool success,
            string message,
            string mobile,
            string draftName = null,
            string draftEmail = null,
            bool keepOtpRow = false)
        {
            if (!forRegister || WantsJsonResponse())
            {
                return Json(new { success, message });
            }

            StashRegisterDraft(draftName, draftEmail);
            TempData["RegisterPhone"] = mobile;
            if (!success)
            {
                TempData["RegisterOtpError"] = message;
                if (keepOtpRow)
                {
                    TempData["RegisterOtpSent"] = true;
                }
            }
            else
            {
                TempData["RegisterPhoneVerified"] = true;
            }

            return RedirectToAction("Register", "Customer");
        }
    }
}
