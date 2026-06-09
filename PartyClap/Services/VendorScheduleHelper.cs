using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PartyClap.Models;

namespace PartyClap.Services
{
    public static class VendorScheduleHelper
    {
        /// <summary>Minutes after a slot ends before it reopens for the next booking.</summary>
        public const int SlotReopenBufferMinutes = 60;

        private static readonly HashSet<string> BlockingSlotTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "booked",
            "pending",
            "blocked",
            "partyclap"
        };
        public static object ToSlotPayload(VendorScheduleSlot s) => new
        {
            blockId = s.BlockId,
            slotType = s.SlotType,
            startTime = s.StartTime,
            endTime = s.EndTime,
            label = s.Label,
            isHourly = s.IsHourly,
            editable = s.Editable,
            customer = s.CustomerName,
            customerPhone = s.CustomerPhone,
            customerEmail = s.CustomerEmail,
            serviceType = s.ServiceType,
            eventType = s.EventType,
            partyLocation = s.PartyLocation,
            partyPinCode = s.PartyPinCode,
            source = s.Source,
            status = s.Status,
            relatedId = s.RelatedId,
            totalCost = s.TotalCost,
            guestCount = s.GuestCount
        };

        public static object ToCalendarPayload(VendorScheduleEntry e) => new
        {
            date = e.Date.ToString("yyyy-MM-dd"),
            booked = e.IsBooked,
            underProcess = e.IsUnderProcess,
            label = e.Label,
            startTime = e.StartTime,
            endTime = e.EndTime,
            source = e.Source,
            customer = e.CustomerName,
            customerPhone = e.CustomerPhone,
            customerEmail = e.CustomerEmail,
            status = e.Status,
            eventType = e.EventType,
            serviceType = e.ServiceType,
            partyLocation = e.PartyLocation,
            partyPinCode = e.PartyPinCode,
            eventRangeStart = e.EventRangeStart,
            eventRangeEnd = e.EventRangeEnd,
            dayCount = e.DayCount,
            guestCount = e.GuestCount,
            totalCost = e.TotalCost,
            relatedId = e.RelatedId,
            slots = (e.Slots ?? new List<VendorScheduleSlot>()).Select(ToSlotPayload).ToList()
        };

        public static IEnumerable<object> ToCalendarPayload(IEnumerable<VendorScheduleEntry> entries)
            => entries.Select(ToCalendarPayload);
        private static readonly HashSet<string> NonBlockingStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Cancelled",
            "Rejected",
            "Completed"
        };

        public static bool IsBlockingStatus(string status)
        {
            return !string.IsNullOrWhiteSpace(status) && !NonBlockingStatuses.Contains(status.Trim());
        }

        /// <summary>True when a booking has completed customer payment and should block the calendar as booked.</summary>
        public static bool IsBookingPaidForSchedule(string status, decimal advancePaid, decimal customerTotal, bool balancePaidOnApp)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;

            var normalized = status.Trim();
            if (!normalized.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)
                && !normalized.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (customerTotal <= 0)
            {
                return balancePaidOnApp && advancePaid > 0;
            }

            return advancePaid >= customerTotal || balancePaidOnApp;
        }

        public static bool IsScheduleBooked(string status, string source, decimal advancePaid, decimal customerTotal, bool balancePaidOnApp)
        {
            if (string.Equals(source, "Service Request", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return IsBookingPaidForSchedule(status, advancePaid, customerTotal, balancePaidOnApp);
        }

        public static bool IsScheduleUnderProcess(string status, string source, decimal advancePaid, decimal customerTotal, bool balancePaidOnApp)
        {
            if (IsScheduleBooked(status, source, advancePaid, customerTotal, balancePaidOnApp))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(status)) return false;

            var normalized = status.Trim();
            if (normalized.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Expired", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(source, "Service Request", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return normalized.Equals("UnderProcess", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Requested", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Confirmed", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBlockingSlotType(string slotType)
            => !string.IsNullOrWhiteSpace(slotType) && BlockingSlotTypes.Contains(slotType.Trim());

        public static bool IsPartyClapBlockLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return true;
            var trimmed = label.Trim();
            return trimmed.Equals("Partyclap", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("PartyClap", StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveManualBlockSlotType(string label)
            => IsPartyClapBlockLabel(label) ? "partyclap" : "blocked";

        public static DateTime? ParseTimeOnDate(DateTime date, string time)
        {
            if (string.IsNullOrWhiteSpace(time)) return null;
            var parts = time.Trim().Split(':');
            if (parts.Length < 2) return null;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours)) return null;
            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)) return null;
            if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) return null;
            return date.Date.AddHours(hours).AddMinutes(minutes);
        }

        public static DateTime? GetSlotEndDateTime(DateTime entryDate, VendorScheduleSlot slot)
        {
            if (slot == null) return null;

            if (!string.IsNullOrWhiteSpace(slot.EndTime))
            {
                return ParseTimeOnDate(entryDate, slot.EndTime);
            }

            if (!string.IsNullOrWhiteSpace(slot.StartTime))
            {
                var start = ParseTimeOnDate(entryDate, slot.StartTime);
                if (start == null) return entryDate.Date.AddDays(1).AddTicks(-1);
                if (slot.IsHourly)
                {
                    return start.Value.AddHours(1);
                }

                return entryDate.Date.AddDays(1).AddTicks(-1);
            }

            return entryDate.Date.AddDays(1).AddTicks(-1);
        }

        public static bool IsSlotExpired(VendorScheduleSlot slot, DateTime entryDate, DateTime? now = null)
        {
            if (slot == null || !IsBlockingSlotType(slot.SlotType)) return false;

            now ??= DateTime.Now;
            if (entryDate.Date > now.Value.Date) return false;
            if (entryDate.Date < now.Value.Date) return true;

            var end = GetSlotEndDateTime(entryDate, slot);
            if (!end.HasValue) return false;
            return now.Value >= end.Value.AddMinutes(SlotReopenBufferMinutes);
        }

        public static void ApplySlotExpiryRules(IEnumerable<VendorScheduleEntry> entries, DateTime? now = null)
        {
            now ??= DateTime.Now;
            foreach (var entry in entries ?? Enumerable.Empty<VendorScheduleEntry>())
            {
                ApplySlotExpiryRules(entry, now);
            }
        }

        public static void ApplySlotExpiryRules(VendorScheduleEntry entry, DateTime? now = null)
        {
            now ??= DateTime.Now;
            if (entry == null) return;
            entry.Slots ??= new List<VendorScheduleSlot>();

            foreach (var slot in entry.Slots)
            {
                if (!IsBlockingSlotType(slot.SlotType) || !IsSlotExpired(slot, entry.Date, now)) continue;
                slot.SlotType = "completed";
                slot.Status = "Completed";
                slot.Editable = false;
            }

            var activeBlocking = entry.Slots.Where(s => IsBlockingSlotType(s.SlotType)).ToList();
            if (!activeBlocking.Any())
            {
                entry.IsBooked = false;
                entry.IsUnderProcess = false;
                entry.Source = null;
                entry.Status = null;
                entry.CustomerName = null;
                entry.CustomerPhone = null;
                entry.CustomerEmail = null;
                entry.EventType = null;
                entry.ServiceType = null;
                entry.Label = null;
                entry.StartTime = null;
                entry.EndTime = null;
                entry.PartyLocation = null;
                entry.PartyPinCode = null;
                entry.EventRangeStart = null;
                entry.EventRangeEnd = null;
                entry.DayCount = null;
                entry.GuestCount = null;
                entry.TotalCost = null;
                entry.RelatedId = null;

                if (entry.Date.Date == now.Value.Date)
                {
                    AddAutoAvailableSlotIfReady(entry, now.Value);
                }
            }
            else
            {
                entry.IsBooked = activeBlocking.Any(s =>
                    string.Equals(s.SlotType, "booked", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s.SlotType, "blocked", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s.SlotType, "partyclap", StringComparison.OrdinalIgnoreCase));
                entry.IsUnderProcess = activeBlocking.Any(s =>
                    string.Equals(s.SlotType, "pending", StringComparison.OrdinalIgnoreCase));

                var primary = activeBlocking[0];
                entry.Source = primary.Source;
                entry.Status = primary.Status;
                entry.Label = primary.Label ?? primary.CustomerName;
                entry.StartTime = primary.StartTime;
                entry.EndTime = primary.EndTime;
                entry.CustomerName = primary.CustomerName;
                entry.CustomerPhone = primary.CustomerPhone;
                entry.CustomerEmail = primary.CustomerEmail;
                entry.ServiceType = primary.ServiceType;
                entry.EventType = primary.EventType;
                entry.PartyLocation = primary.PartyLocation;
                entry.PartyPinCode = primary.PartyPinCode;
                entry.RelatedId = primary.RelatedId;
                entry.TotalCost = primary.TotalCost;
                entry.GuestCount = primary.GuestCount;
            }
        }

        private static void AddAutoAvailableSlotIfReady(VendorScheduleEntry entry, DateTime now)
        {
            if (entry.Slots.Any(s => string.Equals(s.SlotType, "available", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var completedSlots = entry.Slots
                .Where(s => string.Equals(s.SlotType, "completed", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (!completedSlots.Any())
            {
                return;
            }

            DateTime? latestReopen = null;
            foreach (var slot in completedSlots)
            {
                var end = GetSlotEndDateTime(entry.Date, slot);
                if (!end.HasValue) continue;
                var reopen = end.Value.AddMinutes(SlotReopenBufferMinutes);
                if (!latestReopen.HasValue || reopen > latestReopen)
                {
                    latestReopen = reopen;
                }
            }

            if (!latestReopen.HasValue || now < latestReopen.Value)
            {
                return;
            }

            var openFrom = latestReopen.Value.ToString(@"HH\:mm", CultureInfo.InvariantCulture);
            entry.Slots.Add(new VendorScheduleSlot
            {
                SlotType = "available",
                StartTime = openFrom,
                EndTime = "23:59",
                Label = "Open for booking",
                Status = "Available",
                Source = "Auto"
            });
            entry.StartTime = openFrom;
            entry.EndTime = "23:59";
            entry.Label = "Open for booking";
        }
    }
}
