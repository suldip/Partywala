using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PartyClap.Models;
using PartyClap.Services;
using Microsoft.AspNetCore.Http;

namespace PartyClap.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IDataService _dataService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAppSettingsService _appSettings;

        public AdminController(IDataService dataService, IPasswordHasher passwordHasher, IAppSettingsService appSettings)
        {
            _dataService = dataService;
            _passwordHasher = passwordHasher;
            _appSettings = appSettings;
        }

        /// <summary>
        /// Defense-in-depth: block customers/vendors even if cookie/session state is inconsistent.
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var allowsAnonymous = context.ActionDescriptor.EndpointMetadata
                .Any(metadata => metadata is IAllowAnonymous);
            if (allowsAnonymous)
            {
                base.OnActionExecuting(context);
                return;
            }

            if (!User.IsInRole("Admin"))
            {
                if (User.IsInRole("Customer"))
                {
                    TempData["ErrorMessage"] = "You do not have permission to access the admin area.";
                    context.Result = RedirectToAction("Dashboard", "Customer");
                    return;
                }
                if (User.IsInRole("Vendor"))
                {
                    TempData["ErrorMessage"] = "You do not have permission to access the admin area.";
                    context.Result = RedirectToAction("Dashboard", "Vendor");
                    return;
                }
                context.Result = RedirectToAction("Login", "Account");
                return;
            }

            base.OnActionExecuting(context);
        }

        public IActionResult Settings()
        {
            ViewBag.PlatformFeePercent = _appSettings.GetPlatformFeePercent();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(decimal platformFeePercent)
        {
            if (platformFeePercent < 0 || platformFeePercent > 100)
            {
                TempData["ErrorMessage"] = "Platform fee must be between 0 and 100.";
            }
            else
            {
                _appSettings.SetPlatformFeePercent(platformFeePercent);
                TempData["SuccessMessage"] = $"Platform fee updated to {platformFeePercent}%.";
            }
            return RedirectToAction("Settings");
        }

        public IActionResult Dashboard(string vendorId = null)
        {
            var vendors = _dataService.GetAllVendors();
            var scheduleFrom = DateTime.Today;
            var scheduleTo = DateTime.Today.AddDays(60);
            var calendarSummaries = _dataService.GetVendorCalendarSummaries(scheduleFrom, scheduleTo);

            ViewBag.TotalVendors = vendors?.Count ?? 0;
            ViewBag.TotalBookings = calendarSummaries.Sum(s => s.BookedDays);
            ViewBag.TotalRevenue = 0;
            ViewBag.PlatformFeePercent = _appSettings.GetPlatformFeePercent();
            ViewBag.VendorCalendarSummaries = calendarSummaries;
            ViewBag.ScheduleFrom = scheduleFrom;
            ViewBag.ScheduleTo = scheduleTo;

            var selectedVendorId = vendorId;
            if (string.IsNullOrWhiteSpace(selectedVendorId) && calendarSummaries.Any())
            {
                selectedVendorId = calendarSummaries.First().VendorId;
            }

            ViewBag.SelectedVendorId = selectedVendorId;
            if (!string.IsNullOrWhiteSpace(selectedVendorId))
            {
                var vendor = _dataService.GetVendor(selectedVendorId);
                var schedule = _dataService.GetVendorSchedule(selectedVendorId, scheduleFrom, scheduleTo);
                var blocks = _dataService.GetVendorCalendarBlocks(selectedVendorId, scheduleFrom, scheduleTo);
                ViewBag.SelectedVendor = vendor;
                ViewBag.SelectedVendorSchedule = schedule;
                ViewBag.SelectedVendorBlocks = blocks;
            }

            var allowedPinCodes = _dataService.GetAllowedPinCodes();
            ViewBag.AllowedPinCount = allowedPinCodes != null ? allowedPinCodes.Count : 0;

            var states = _dataService.GetAllStates();
            ViewBag.TotalStateCount = states?.Count ?? 0;
            ViewBag.EnabledStateCount = states?.Count(s => s.IsEnabled) ?? 0;

            return View();
        }

        [HttpGet]
        public IActionResult GetVendorScheduleJson(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
            {
                return Json(new { success = false, message = "Vendor is required." });
            }

            var vendor = _dataService.GetVendor(vendorId);
            if (vendor == null)
            {
                return Json(new { success = false, message = "Vendor not found." });
            }

            var from = DateTime.Today;
            var to = DateTime.Today.AddDays(60);
            var schedule = _dataService.GetVendorSchedule(vendorId, from, to);
            var blocks = _dataService.GetVendorCalendarBlocks(vendorId, from, to);

            return Json(new
            {
                success = true,
                vendor = new
                {
                    id = vendor.Id,
                    name = vendor.Name,
                    phone = vendor.Phone,
                    pinCode = vendor.PinCode,
                    address = vendor.Address,
                    isRegistered = vendor.IsRegistered
                },
                schedule = VendorScheduleHelper.ToCalendarPayload(schedule),
                blocks = blocks.Select(b => new
                {
                    id = b.Id,
                    date = b.BlockDate.ToString("yyyy-MM-dd"),
                    startTime = b.StartTime,
                    endTime = b.EndTime,
                    isAvailable = b.IsAvailable,
                    label = b.Label
                }),
                stats = new
                {
                    booked = schedule.Count(e => e.Date >= DateTime.Today && e.IsBooked),
                    underProcess = schedule.Count(e => e.Date >= DateTime.Today && e.IsUnderProcess),
                    available = schedule.Count(e => !e.IsBooked && !e.IsUnderProcess && e.Date >= DateTime.Today),
                    manual = blocks.Count(b => !b.IsAvailable)
                }
            });
        }

        public IActionResult ManagePinCodes()
        {
            var codes = _dataService.GetAllowedPinCodes();
            return View(codes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddPinCode(string pinCode, string cityName)
        {
            if (!string.IsNullOrEmpty(pinCode))
            {
                _dataService.AddAllowedPinCode(pinCode, cityName);
                TempData["SuccessMessage"] = $"PIN code {pinCode} added successfully.";
            }
            return RedirectToAction("ManagePinCodes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePinCode(string pinCode)
        {
            if (!string.IsNullOrEmpty(pinCode))
            {
                _dataService.DeleteAllowedPinCode(pinCode);
                TempData["SuccessMessage"] = $"PIN code {pinCode} removed.";
            }
            return RedirectToAction("ManagePinCodes");
        }

        // ----- State (serviceable-area) management -----

        public IActionResult ManageStates()
        {
            var states = _dataService.GetAllStates();
            return View(states);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleState(int id, bool enabled)
        {
            _dataService.SetStateEnabled(id, enabled);
            TempData["SuccessMessage"] = enabled
                ? "State enabled. Services are now available there."
                : "State disabled. Services are now blocked there.";
            return RedirectToAction("ManageStates");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetAllStates(bool enabled)
        {
            _dataService.SetAllStatesEnabled(enabled);
            TempData["SuccessMessage"] = enabled ? "All states enabled." : "All states disabled.";
            return RedirectToAction("ManageStates");
        }

        // Bulk import of India city/PIN-code data (e.g. the India Post pincode CSV).
        // Recognises columns by header name (any order); falls back to
        // PinCode, AreaName, City, State column order when no header is detected.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(104_857_600)] // 100 MB
        public IActionResult ImportLocations(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please choose a non-empty CSV file to import.";
                return RedirectToAction("ManageStates");
            }

            try
            {
                var rows = ParseLocationCsv(file);
                int imported = _dataService.ImportLocations(rows);
                TempData["SuccessMessage"] = $"Imported {imported:N0} location rows. New states (if any) were added and enabled.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Import failed: " + ex.Message;
            }
            return RedirectToAction("ManageStates");
        }

        private static List<Location> ParseLocationCsv(IFormFile file)
        {
            var rows = new List<Location>();
            using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) return rows;

                var header = SplitCsvLine(headerLine);
                int pinIdx = FindColumn(header, "pincode", "pin code", "pin");
                int areaIdx = FindColumn(header, "officename", "office name", "areaname", "area name", "area");
                int cityIdx = FindColumn(header, "district", "city", "taluk");
                int stateIdx = FindColumn(header, "statename", "state name", "state");

                bool hasHeader = pinIdx >= 0 || stateIdx >= 0;
                if (!hasHeader)
                {
                    // Treat the first line as data using the default column order.
                    pinIdx = 0; areaIdx = 1; cityIdx = 2; stateIdx = 3;
                    AddRow(rows, header, pinIdx, areaIdx, cityIdx, stateIdx);
                }

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cols = SplitCsvLine(line);
                    AddRow(rows, cols, pinIdx, areaIdx, cityIdx, stateIdx);
                }
            }
            return rows;
        }

        private static void AddRow(List<Location> rows, List<string> cols, int pinIdx, int areaIdx, int cityIdx, int stateIdx)
        {
            string pin = Get(cols, pinIdx);
            if (string.IsNullOrWhiteSpace(pin)) return;
            rows.Add(new Location
            {
                PinCode = pin.Trim(),
                AreaName = Get(cols, areaIdx),
                City = Get(cols, cityIdx),
                State = Get(cols, stateIdx)
            });
        }

        private static string Get(List<string> cols, int idx)
            => idx >= 0 && idx < cols.Count ? cols[idx].Trim() : "";

        private static int FindColumn(List<string> header, params string[] names)
        {
            for (int i = 0; i < header.Count; i++)
            {
                var h = header[i].Trim().Trim('"').ToLowerInvariant();
                foreach (var name in names)
                {
                    if (h == name) return i;
                }
            }
            return -1;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString());
            return result;
        }

        // Admin self-registration is only open for the very first admin (bootstrap).
        // Once an admin exists, only an authenticated admin may create more.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (_dataService.GetAdminCount() > 0 && !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Admin admin)
        {
            if (_dataService.GetAdminCount() > 0 && !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                admin.Id = Guid.NewGuid().ToString();
                admin.PasswordHash = _passwordHasher.Hash(admin.PasswordHash);
                _dataService.RegisterAdmin(admin);
                return RedirectToAction("Login", "Account");
            }
            return View(admin);
        }
    }
}
