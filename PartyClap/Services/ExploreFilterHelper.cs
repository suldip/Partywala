using System.Text.Json;
using PartyClap.Models;

namespace PartyClap.Services
{
    public static class ExploreFilterHelper
    {
        public static string NormalizeCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category) || category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return "all";
            }

            return category.Trim().ToLowerInvariant() switch
            {
                "music" => "singer",
                "catering" => "chef",
                "decoration" => "decorator",
                "photography" => "photographer",
                "magic" => "magician",
                "planning" => "event-manager",
                _ => category.Trim().ToLowerInvariant().Replace(" ", "-")
            };
        }

        public static IReadOnlyList<string> GetCategorySqlPatterns(string normalizedCategory)
        {
            return normalizedCategory switch
            {
                "singer" => new[] { "%sing%", "%music%", "%dj%" },
                "chef" => new[] { "%chef%", "%cater%" },
                "decorator" => new[] { "%decor%" },
                "magician" => new[] { "%magic%" },
                "photographer" => new[] { "%photo%" },
                "event-manager" => new[] { "%event%", "%plan%" },
                "casino" => new[] { "%casino%" },
                _ => new[] { $"%{normalizedCategory.Replace("-", "%")}%" }
            };
        }

        public static string GetCategorySlugs(string? serviceType)
        {
            var type = serviceType?.ToLowerInvariant() ?? string.Empty;
            var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "all" };

            if (type.Contains("sing") || type.Contains("music") || type.Contains("dj")) slugs.Add("singer");
            if (type.Contains("chef") || type.Contains("cater")) slugs.Add("chef");
            if (type.Contains("decor")) slugs.Add("decorator");
            if (type.Contains("magic")) slugs.Add("magician");
            if (type.Contains("photo")) slugs.Add("photographer");
            if (type.Contains("event") || type.Contains("plan")) slugs.Add("event-manager");
            if (type.Contains("casino")) slugs.Add("casino");

            if (slugs.Count == 1 && !string.IsNullOrWhiteSpace(serviceType))
            {
                slugs.Add(serviceType.Trim().ToLowerInvariant().Replace(" ", "-"));
            }

            return string.Join(",", slugs);
        }

        public static int GetReviewCount(ServiceListing service, IReadOnlyDictionary<string, ReviewSummary> summaries = null)
        {
            if (summaries != null && summaries.TryGetValue(service.Id, out var summary) && summary.ReviewCount > 0)
            {
                return summary.ReviewCount;
            }

            return GetReviewCountFromAttributes(service);
        }

        public static double GetRating(ServiceListing service, IReadOnlyDictionary<string, ReviewSummary> summaries = null)
        {
            if (summaries != null && summaries.TryGetValue(service.Id, out var summary) && summary.ReviewCount > 0)
            {
                return summary.AverageRating;
            }

            return GetRatingFromAttributes(service);
        }

        private static double GetRatingFromAttributes(ServiceListing service)
        {
            if (string.IsNullOrEmpty(service.Attributes)) return 0;

            try
            {
                using var json = JsonDocument.Parse(service.Attributes);
                if (json.RootElement.TryGetProperty("rating", out var ratingProp))
                {
                    return ratingProp.ValueKind switch
                    {
                        JsonValueKind.Number => ratingProp.GetDouble(),
                        JsonValueKind.String when double.TryParse(ratingProp.GetString(), out var parsed) => parsed,
                        _ => 0
                    };
                }
            }
            catch
            {
                // Ignore malformed attribute payloads.
            }

            return 0;
        }

        public static int GetReviewCountFromAttributes(ServiceListing service)
        {
            if (string.IsNullOrEmpty(service.Attributes)) return 0;

            try
            {
                using var json = JsonDocument.Parse(service.Attributes);
                if (json.RootElement.TryGetProperty("reviews", out var reviewsProp))
                {
                    return reviewsProp.ValueKind switch
                    {
                        JsonValueKind.Number => reviewsProp.GetInt32(),
                        JsonValueKind.String when int.TryParse(reviewsProp.GetString(), out var parsed) => parsed,
                        _ => 0
                    };
                }
            }
            catch
            {
                // Ignore malformed attribute payloads.
            }

            return 0;
        }

        public static List<ServiceListing> ApplyRatingAndSort(
            List<ServiceListing> services,
            decimal? minRating,
            string? sortBy,
            IReadOnlyDictionary<string, ReviewSummary> summaries = null)
        {
            IEnumerable<ServiceListing> query = services;

            if (minRating.HasValue && minRating.Value > 0)
            {
                var threshold = (double)minRating.Value;
                query = query.Where(s => GetRating(s, summaries) >= threshold);
            }

            query = (sortBy ?? "rating").ToLowerInvariant() switch
            {
                "price-low" => query.OrderBy(s => s.Cost),
                "price-high" => query.OrderByDescending(s => s.Cost),
                "reviews" => query.OrderByDescending(s => GetReviewCount(s, summaries)),
                _ => query.OrderByDescending(s => GetRating(s, summaries)).ThenByDescending(s => GetReviewCount(s, summaries))
            };

            return query.ToList();
        }
    }
}
