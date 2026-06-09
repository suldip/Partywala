using System;
using System.Collections.Generic;
using System.Linq;
using PartyClap.Models;

namespace PartyClap.Services
{
    public static class ServiceDedupHelper
    {
        public static string BuildFingerprint(ServiceListing service)
        {
            if (service == null)
            {
                return string.Empty;
            }

            var type = (service.ServiceType ?? string.Empty).Trim().ToLowerInvariant();
            var description = (service.Description ?? string.Empty).Trim().ToLowerInvariant();
            var unit = (service.Unit ?? string.Empty).Trim().ToLowerInvariant();
            return $"{type}|{description}|{unit}|{service.Cost:0.00}";
        }

        public static bool AreEquivalent(ServiceListing left, ServiceListing right)
        {
            return string.Equals(BuildFingerprint(left), BuildFingerprint(right), StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns one row per equivalent service, keeping the oldest Id alphabetically.
        /// </summary>
        public static List<ServiceListing> Deduplicate(IEnumerable<ServiceListing> services)
        {
            return (services ?? Enumerable.Empty<ServiceListing>())
                .Where(s => s != null)
                .GroupBy(BuildFingerprint, StringComparer.Ordinal)
                .Select(group => group.OrderBy(s => s.Id, StringComparer.Ordinal).First())
                .OrderBy(s => s.ServiceType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.Cost)
                .ToList();
        }
    }
}
