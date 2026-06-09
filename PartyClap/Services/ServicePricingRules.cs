using System;

namespace PartyClap.Services
{
    public static class ServicePricingRules
    {
        public const decimal MinimumCost = 1m;

        /// <summary>Returns an error message when invalid, or null when acceptable.</summary>
        public static string ValidateCost(decimal cost)
        {
            if (cost < MinimumCost)
            {
                return $"Price must be at least ₹{MinimumCost:N0}. Zero or pending prices cannot be published.";
            }
            return null;
        }

        /// <summary>Optional weekend price — when set, must meet the same minimum.</summary>
        public static string ValidateWeekendCost(decimal? weekendCost)
        {
            if (!weekendCost.HasValue) return null;
            if (weekendCost.Value < MinimumCost)
            {
                return $"Weekend price must be at least ₹{MinimumCost:N0} when provided.";
            }
            return null;
        }
    }
}
