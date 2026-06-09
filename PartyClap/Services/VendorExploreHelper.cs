using PartyClap.Models;

namespace PartyClap.Services
{
    public static class VendorExploreHelper
    {
        public static void EnsurePinCodeListed(IDataService dataService, string pinCode, string cityName = null)
        {
            if (string.IsNullOrWhiteSpace(pinCode))
            {
                return;
            }

            pinCode = pinCode.Trim();
            if (pinCode.Length != 6)
            {
                return;
            }

            var label = string.IsNullOrWhiteSpace(cityName) ? $"Area {pinCode}" : cityName.Trim();
            dataService.AddAllowedPinCode(pinCode, label);
        }

        public static void EnsureVendorListed(IDataService dataService, Vendor vendor)
        {
            if (vendor == null)
            {
                return;
            }

            EnsurePinCodeListed(dataService, vendor.PinCode);

            if (vendor.ServiceLocations != null)
            {
                foreach (var pin in vendor.ServiceLocations)
                {
                    EnsurePinCodeListed(dataService, pin);
                }
            }
        }
    }
}
