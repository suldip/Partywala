using System;
using System.Collections.Generic;
using PartyClap.Models;

namespace PartyClap.Services
{
    public interface IPricingService
    {
        decimal GetEffectiveBaseCost(CartItem item);
        int CountEventDays(DateTime startDate, DateTime endDate);
        decimal CalculateMultiDayCost(decimal dailyRate, decimal? weekendRate, DateTime startDate, DateTime endDate);
        decimal CalculateCartItemTotal(CartItem item);
        decimal GetPlatformFeePercent();
        decimal GetGstPercent();
        BookingPricing CalculatePricing(decimal baseCost);
        bool IsWeekend(DateTime date);
        LoyaltySummary CalculateLoyalty(IEnumerable<Booking> bookings);
    }

    public class BookingPricing
    {
        public decimal VendorCost { get; set; }
        public decimal Subtotal { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstAmount { get; set; }
        public decimal CustomerTotalCost { get; set; }
        public decimal AdvancePaid { get; set; }
        public decimal BalanceAmount { get; set; }
    }

    public class LoyaltySummary
    {
        public int PartyPoints { get; set; }
        public string Tier { get; set; }
        public int NextTierPoints { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
