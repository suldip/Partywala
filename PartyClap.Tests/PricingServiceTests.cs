using System;
using System.Collections.Generic;
using PartyClap.Models;
using PartyClap.Services;
using Xunit;

namespace PartyClap.Tests
{
    public class PricingServiceTests
    {
        private readonly IPricingService _pricing = new PricingService(new FixedPlatformFeeSettings(10m));

        private sealed class FixedPlatformFeeSettings : IAppSettingsService
        {
            private readonly decimal _percent;

            public FixedPlatformFeeSettings(decimal percent) => _percent = percent;

            public decimal GetPlatformFeePercent() => _percent;

            public void SetPlatformFeePercent(decimal percent) { }

            public string GetValue(string key, string defaultValue = null) => defaultValue;

            public void SetValue(string key, string value) { }
        }

        [Fact]
        public void CalculatePricing_AppliesMarkupAndAdvance()
        {
            var result = _pricing.CalculatePricing(1000m);

            Assert.Equal(1000m, result.VendorCost);
            Assert.Equal(1100m, result.CustomerTotalCost);   // 10% markup
            Assert.Equal(200m, result.AdvancePaid);          // 20% advance
            Assert.Equal(900m, result.BalanceAmount);        // total - advance
        }

        [Fact]
        public void GetEffectiveBaseCost_UsesWeekendCost_OnWeekend()
        {
            // 2026-05-30 is a Saturday.
            var item = new CartItem { Cost = 1000m, WeekendCost = 1500m, EventDate = new DateTime(2026, 5, 30) };

            Assert.Equal(1500m, _pricing.GetEffectiveBaseCost(item));
        }

        [Fact]
        public void GetEffectiveBaseCost_UsesBaseCost_OnWeekday()
        {
            // 2026-06-01 is a Monday.
            var item = new CartItem { Cost = 1000m, WeekendCost = 1500m, EventDate = new DateTime(2026, 6, 1) };

            Assert.Equal(1000m, _pricing.GetEffectiveBaseCost(item));
        }

        [Fact]
        public void CalculateLoyalty_BronzeTier_ForNoBookings()
        {
            var summary = _pricing.CalculateLoyalty(new List<Booking>());

            Assert.Equal("Bronze", summary.Tier);
            Assert.Equal(0, summary.PartyPoints);
        }

        [Fact]
        public void CalculateLoyalty_AwardsPointsForConfirmedBookings()
        {
            var bookings = new List<Booking>
            {
                new Booking { Status = "Confirmed", CustomerTotalCost = 10000m },
                new Booking { Status = "Requested", CustomerTotalCost = 5000m } // ignored
            };

            var summary = _pricing.CalculateLoyalty(bookings);

            // 50 (per booking) + 10000/100 = 150 points
            Assert.Equal(150, summary.PartyPoints);
            Assert.Equal(10000m, summary.TotalSpent);
        }
    }
}
