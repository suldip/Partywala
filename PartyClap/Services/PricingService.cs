using System;
using System.Collections.Generic;
using PartyClap.Models;

namespace PartyClap.Services
{
    /// <summary>
    /// Centralizes booking pricing and customer-loyalty calculations that were
    /// previously embedded in controllers, so the rules live in one tested place.
    /// </summary>
    public class PricingService : IPricingService
    {
        public const decimal AdvanceRate = 0.20m;       // 20% advance

        private readonly IAppSettingsService _settings;

        public PricingService(IAppSettingsService settings)
        {
            _settings = settings;
        }

        /// <summary>Admin-configurable platform fee as a percentage (e.g. 10 = 10%).</summary>
        public decimal GetPlatformFeePercent() => _settings.GetPlatformFeePercent();

        public decimal GetGstPercent() => _settings.GetGstPercent();

        public decimal GetEffectiveBaseCost(CartItem item)
        {
            if (item == null) return 0m;

            decimal baseCost = item.Cost;
            if (item.EventDate.HasValue && item.WeekendCost.HasValue && IsWeekend(item.EventDate.Value))
            {
                baseCost = item.WeekendCost.Value;
            }
            return baseCost;
        }

        public int CountEventDays(DateTime startDate, DateTime endDate)
        {
            if (endDate.Date < startDate.Date) return 0;
            return (endDate.Date - startDate.Date).Days + 1;
        }

        public decimal CalculateMultiDayCost(decimal dailyRate, decimal? weekendRate, DateTime startDate, DateTime endDate)
        {
            if (endDate.Date < startDate.Date) return dailyRate;

            decimal total = 0m;
            for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
            {
                total += weekendRate.HasValue && IsWeekend(day) ? weekendRate.Value : dailyRate;
            }
            return total;
        }

        public decimal CalculateCartItemTotal(CartItem item)
        {
            if (item == null) return 0m;
            if (!item.EventDate.HasValue) return item.Cost;

            var endDate = item.EventEndDate ?? item.EventDate.Value;
            return CalculateMultiDayCost(item.Cost, item.WeekendCost, item.EventDate.Value, endDate);
        }

        public BookingPricing CalculatePricing(decimal baseCost)
        {
            var gstPercent = GetGstPercent();
            var subtotal = baseCost;
            var gstAmount = Math.Round(subtotal * gstPercent / 100m, 0, MidpointRounding.AwayFromZero);
            var customerTotal = subtotal + gstAmount;
            var advance = customerTotal * AdvanceRate;
            return new BookingPricing
            {
                VendorCost = baseCost,
                Subtotal = subtotal,
                GstPercent = gstPercent,
                GstAmount = gstAmount,
                CustomerTotalCost = customerTotal,
                AdvancePaid = advance,
                BalanceAmount = customerTotal - advance
            };
        }

        public bool IsWeekend(DateTime date)
            => date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        public LoyaltySummary CalculateLoyalty(IEnumerable<Booking> bookings)
        {
            int partyPoints = 0;
            decimal totalSpent = 0m;

            if (bookings != null)
            {
                foreach (var b in bookings)
                {
                    if (b.Status == "Confirmed" || b.BalancePaidOnApp)
                    {
                        totalSpent += b.CustomerTotalCost;
                        partyPoints += 50; // 50 points per booking
                    }
                }
                partyPoints += (int)(totalSpent / 100); // 1 point per ₹100 spent
            }

            string tier = "Bronze";
            int pointsForNext = 500;
            if (partyPoints >= 2000)
            {
                tier = "Gold";
                pointsForNext = 5000;
            }
            else if (partyPoints >= 500)
            {
                tier = "Silver";
                pointsForNext = 2000;
            }

            return new LoyaltySummary
            {
                PartyPoints = partyPoints,
                Tier = tier,
                NextTierPoints = pointsForNext,
                TotalSpent = totalSpent
            };
        }
    }
}
