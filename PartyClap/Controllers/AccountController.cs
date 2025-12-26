using Microsoft.AspNetCore.Mvc;
using PartyClap.Services;
using PartyClap.Models;
using System.Data;
using MySql.Data.MySqlClient;

namespace PartyClap.Controllers
{
    public class AccountController : Controller
    {
        private readonly IDataService _dataService;

        public AccountController(IDataService dataService)
        {
            _dataService = dataService;
        }
        
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SendOtp(string mobile)
        {
            if (string.IsNullOrEmpty(mobile))
            {
                return Json(new { success = false, message = "Mobile number is required" });
            }

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            
            // Store in Session
            HttpContext.Session.SetString("OTP_" + mobile, otp);
            
            // In a real app, send via SMS Service. Here we return it for testing.
            return Json(new { success = true, message = "OTP sent successfully", debugOtp = otp });
        }

        [HttpPost]
        public IActionResult Login(string mobile, string otp)
        {
            ViewBag.Mobile = mobile;

            // 1. Check Admin (Login via Email & Password using the same fields)
            if (mobile.Contains("@"))
            {
                var admin = _dataService.GetAdminByEmail(mobile);
                if (admin != null)
                {
                    // For Admin, we treat the 'otp' field as the password
                    if (admin.PasswordHash == otp) 
                    {
                        HttpContext.Session.SetString("UserRole", "Admin");
                        HttpContext.Session.SetString("UserId", admin.Id);
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else
                    {
                        ViewBag.Error = "Invalid Admin Credentials";
                        return View();
                    }
                }
                
                // If email provided but not found as admin
                ViewBag.Error = "Admin user not found.";
                return View();
            }

            // 2. Validate OTP (For Customers/Vendors)
            var storedOtp = HttpContext.Session.GetString("OTP_" + mobile);
            if (string.IsNullOrEmpty(storedOtp) || storedOtp != otp)
            {
                ViewBag.Error = "Invalid or expired OTP";
                return View();
            }

            // 3. Check Vendor
            var vendor = _dataService.GetVendorByPhone(mobile);
            if (vendor != null)
            {
                HttpContext.Session.SetString("UserRole", "Vendor");
                HttpContext.Session.SetString("UserId", vendor.Id);
                
                // Clear OTP
                HttpContext.Session.Remove("OTP_" + mobile);
                
                return RedirectToAction("Dashboard", "Vendor");
            }

            // 4. Check Customer
            var customer = _dataService.GetCustomerByPhone(mobile);
            if (customer != null)
            {
                HttpContext.Session.SetString("UserRole", "Customer");
                HttpContext.Session.SetString("UserId", customer.Id);
                
                // Clear OTP
                HttpContext.Session.Remove("OTP_" + mobile);
                
                return RedirectToAction("Dashboard", "Customer");
            }
            
            ViewBag.Error = "User not found with this mobile number.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
