using System;
using Microsoft.AspNetCore.Mvc;
using PartyClap.Models;
using PartyClap.Services;

namespace PartyClap.Controllers
{
    public class AdminController : Controller
    {
        private readonly IDataService _dataService;

        public AdminController(IDataService dataService)
        {
            _dataService = dataService;
        }

        public IActionResult Dashboard()
        {
            // In a real app, check session/cookie for Admin role
            // var role = HttpContext.Session.GetString("UserRole");
            // if (role != "Admin") return RedirectToAction("Login", "Account");

            // Fetch stats (Mock for now)
            ViewBag.TotalVendors = 150;
            ViewBag.TotalBookings = 342;
            ViewBag.TotalRevenue = 500000;
            
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Admin admin)
        {
            // Basic validation
            if (ModelState.IsValid)
            {
                admin.Id = Guid.NewGuid().ToString();
                // In real app, hash password
                _dataService.RegisterAdmin(admin);
                return RedirectToAction("Login", "Account");
            }
            return View(admin);
        }
    }
}
