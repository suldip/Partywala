using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using PartyClap.DAL;

namespace PartyClap.Services
{
    public interface IAppSettingsService
    {
        decimal GetPlatformFeePercent();
        decimal GetGstPercent();
        void SetPlatformFeePercent(decimal percent);
        string GetValue(string key, string defaultValue = null);
        void SetValue(string key, string value);
    }

    /// <summary>
    /// Singleton facade over the (scoped) SettingsDAL. Resolves a scoped DAL via
    /// IServiceScopeFactory and caches values briefly so hot paths (pricing) don't
    /// hit the database on every request.
    /// </summary>
    public class AppSettingsService : IAppSettingsService
    {
        public const string PlatformFeeKey = "PlatformFeePercent";
        public const string GstPercentKey = "GstPercent";
        public const decimal DefaultPlatformFeePercent = 10m;
        public const decimal DefaultGstPercent = 18m;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;

        public AppSettingsService(IServiceScopeFactory scopeFactory, IMemoryCache cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        public decimal GetPlatformFeePercent()
        {
            var raw = GetValue(PlatformFeeKey, DefaultPlatformFeePercent.ToString());
            if (decimal.TryParse(raw, out var percent) && percent >= 0 && percent <= 100)
            {
                return percent;
            }
            return DefaultPlatformFeePercent;
        }

        public void SetPlatformFeePercent(decimal percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            SetValue(PlatformFeeKey, percent.ToString());
            _cache.Remove("appsetting:" + PlatformFeeKey);
        }

        public decimal GetGstPercent()
        {
            var raw = GetValue(GstPercentKey, DefaultGstPercent.ToString());
            if (decimal.TryParse(raw, out var percent) && percent >= 0 && percent <= 100)
            {
                return percent;
            }
            return DefaultGstPercent;
        }

        public string GetValue(string key, string defaultValue = null)
        {
            var cacheKey = "appsetting:" + key;
            if (_cache.TryGetValue(cacheKey, out string cached))
            {
                return cached;
            }

            string value;
            using (var scope = _scopeFactory.CreateScope())
            {
                var dal = scope.ServiceProvider.GetRequiredService<SettingsDAL>();
                value = dal.GetValue(key, defaultValue);
            }

            _cache.Set(cacheKey, value, TimeSpan.FromMinutes(2));
            return value;
        }

        public void SetValue(string key, string value)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dal = scope.ServiceProvider.GetRequiredService<SettingsDAL>();
                dal.SetValue(key, value);
            }
            _cache.Set("appsetting:" + key, value, TimeSpan.FromMinutes(2));
        }
    }
}
