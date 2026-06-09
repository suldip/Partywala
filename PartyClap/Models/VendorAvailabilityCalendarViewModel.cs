using System;
using System.Collections.Generic;
using System.Linq;

namespace PartyClap.Models
{
    public class VendorAvailabilityCalendarViewModel
    {
        public string ElementId { get; set; } = "vendor-teams-calendar";
        public string ScheduleJson { get; set; } = "[]";
        public List<VendorCalendarBlock> ManualBlocks { get; set; } = new List<VendorCalendarBlock>();
        public List<VendorScheduleEntry> ScheduleEntries { get; set; } = new List<VendorScheduleEntry>();
        public bool ReadOnly { get; set; }
        public bool ShowStatCards { get; set; } = true;
        public bool ShowDayPanel { get; set; }
        public bool ShowManualEntries { get; set; } = true;

        public int StatOpenDays =>
            ScheduleEntries.Count(e => e.Date >= DateTime.Today && !e.IsBooked && !e.IsUnderProcess);

        public int StatBookedDays =>
            ScheduleEntries.Count(e => e.Date >= DateTime.Today && e.IsBooked);

        public int StatPendingDays =>
            ScheduleEntries.Count(e => e.Date >= DateTime.Today && e.IsUnderProcess);

        public int StatManualEntries => ManualBlocks.Count(b => !b.IsAvailable);
    }
}
