using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PartyClap.Models;

namespace PartyClap.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Book Party Vendors & Event Services";
            return View();
        }

        public IActionResult Privacy()
        {
            ViewData["Title"] = "Privacy Policy";
            return View();
        }

        public IActionResult TrustCenter()
        {
            ViewData["Title"] = "Trust Center & Dispute Resolution";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact Us";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Unhandled application error. RequestId: {RequestId}", requestId);

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            ViewData["Title"] = "Something Went Wrong";
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult PageNotFound(int statusCode = StatusCodes.Status404NotFound)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            ViewData["Title"] = "Page Not Found";
            ViewData["Message"] = "The page you're looking for doesn't exist or may have been removed.";
            return View("NotFound");
        }
    }
}
