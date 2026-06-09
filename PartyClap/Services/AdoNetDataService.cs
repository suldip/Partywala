using System;
using System.Collections.Generic;
using PartyClap.Models;
using PartyClap.DAL;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace PartyClap.Services
{
    public class AdoNetDataService : IDataService
    {
        private readonly VendorDAL _vendorDAL;
        private readonly CustomerDAL _customerDAL;
        private readonly AdminDAL _adminDAL;
        private readonly ReviewDAL _reviewDAL;
        private readonly CartDAL _cartDAL;
        private readonly MessageDAL _messageDAL;
        private readonly StateDAL _stateDAL;
        private readonly IMemoryCache _cache;

        private const string LocationsCacheKey = "LocationsCache";

        public AdoNetDataService(VendorDAL vendorDAL, CustomerDAL customerDAL, AdminDAL adminDAL, CartDAL cartDAL, ReviewDAL reviewDAL, MessageDAL messageDAL, StateDAL stateDAL, IMemoryCache cache)
        {
            _vendorDAL = vendorDAL;
            _customerDAL = customerDAL;
            _adminDAL = adminDAL;
            _cartDAL = cartDAL;
            _reviewDAL = reviewDAL;
            _messageDAL = messageDAL;
            _stateDAL = stateDAL;
            _cache = cache;
        }

        // Mock properties for backward compatibility
        public List<Vendor> Vendors => throw new NotImplementedException();
        public List<Customer> Customers => throw new NotImplementedException();
        public List<Booking> Bookings => throw new NotImplementedException();

        // Vendor methods
        public void AddVendor(Vendor vendor) => _vendorDAL.RegisterVendor(vendor);
        public List<Vendor> GetAllVendors() => _vendorDAL.GetAllVendors();
        public List<AdminVendorCalendarSummary> GetVendorCalendarSummaries(DateTime fromDate, DateTime toDate)
            => _vendorDAL.GetVendorCalendarSummaries(fromDate, toDate);
        public Vendor GetVendor(string id) => _vendorDAL.GetVendor(id);
        public Vendor GetVendorByEmail(string email) => _vendorDAL.GetVendorByEmail(email);
        public Vendor GetVendorByPhone(string phone) => _vendorDAL.GetVendorByPhone(phone);
        public void UpdateVendor(Vendor vendor) => _vendorDAL.UpdateVendor(vendor);
        
        // Service methods
        public void AddService(ServiceListing service) => _vendorDAL.AddService(service);
        public List<ServiceListing> GetVendorServices(string vendorId) => _vendorDAL.GetVendorServices(vendorId);
        public ServiceListing GetService(string serviceId) => _vendorDAL.GetService(serviceId);
        public void UpdateService(ServiceListing service) => _vendorDAL.UpdateService(service);
        public void DeleteService(string serviceId) => _vendorDAL.DeleteService(serviceId);
        
        // Portfolio methods
        public void AddPortfolioItem(PortfolioItem item) => _vendorDAL.AddPortfolioItem(item);
        public List<PortfolioItem> GetVendorPortfolio(string vendorId) => _vendorDAL.GetVendorPortfolio(vendorId);
        
        // Booking methods
        public void AddBooking(Booking booking) => _vendorDAL.AddBooking(booking);
        public List<Booking> GetVendorBookings(string vendorId) => _vendorDAL.GetVendorBookings(vendorId);
        public Booking GetBooking(string bookingId) => _vendorDAL.GetBooking(bookingId);
        public void UpdateBookingStatus(string bookingId, string status, bool balancePaidOnApp) 
            => _vendorDAL.UpdateBookingStatus(bookingId, status, null, null);
        public void UpdateBookingStatus(string bookingId, string status, decimal? vendorCost = null, decimal? customerTotalCost = null) 
            => _vendorDAL.UpdateBookingStatus(bookingId, status, vendorCost, customerTotalCost);
        public void MarkAdvanceAsPaid(string bookingId) => _vendorDAL.MarkAdvanceAsPaid(bookingId);
        public void MarkBalanceAsPaid(string bookingId) => _vendorDAL.MarkBalanceAsPaid(bookingId);
        
        // Cart methods
        public void AddToCart(string customerId, string cookieId, string serviceId, string vendorId, DateTime? eventDate,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null) 
            => _cartDAL.AddToCart(customerId, cookieId, serviceId, vendorId, eventDate, partyLocation, partyPinCode, partyLatitude, partyLongitude);
        public List<CartItem> GetCartItems(string customerId, string cookieId) => _cartDAL.GetCartItems(customerId, cookieId);
        public void MergeGuestCart(string customerId, string cookieId) => _cartDAL.MergeGuestCart(customerId, cookieId);
        public void RemoveFromCart(int cartItemId) => _cartDAL.RemoveFromCart(cartItemId);
        public void ClearCart(string customerId, string cookieId) => _cartDAL.ClearCart(customerId, cookieId);
        public void UpdateCartItemDate(int cartItemId, DateTime? eventDate) => _cartDAL.UpdateCartItemDate(cartItemId, eventDate);
        public void UpdateCartItemSchedule(int cartItemId, DateTime? eventDate, DateTime? eventEndDate, string startTime, string endTime,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null)
            => _cartDAL.UpdateCartItemSchedule(cartItemId, eventDate, eventEndDate, startTime, endTime, partyLocation, partyPinCode, partyLatitude, partyLongitude);
        public void UpdateBookingSchedule(string bookingId, string startTime, string endTime, DateTime? eventEndDate = null,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null)
            => _cartDAL.UpdateBookingSchedule(bookingId, startTime, endTime, eventEndDate, partyLocation, partyPinCode, partyLatitude, partyLongitude);
        
        // Customer methods
        public void AddCustomer(Customer customer) => _customerDAL.RegisterCustomer(customer);
        public Customer GetCustomer(string id) => throw new NotImplementedException();
        public Customer GetCustomerByEmail(string email) => _customerDAL.GetCustomerByEmail(email);
        public Customer GetCustomerByPhone(string phone) => _customerDAL.GetCustomerByPhone(phone);
        public void RegisterCustomer(Customer customer) => _customerDAL.RegisterCustomer(customer);
        public List<Booking> GetCustomerBookings(string customerId) => _customerDAL.GetCustomerBookings(customerId);
        public void CreateServiceRequest(ServiceRequest request) => _customerDAL.CreateServiceRequest(request);
        public List<ServiceRequest> GetVendorServiceRequests(string vendorId) => _customerDAL.GetVendorServiceRequests(vendorId);
        public List<Dictionary<string, object>> GetVendorServiceRequestsWithDetails(string vendorId) => _customerDAL.GetVendorServiceRequestsWithDetails(vendorId);
        public List<Dictionary<string, object>> GetCustomerServiceRequestsWithDetails(string customerId) => _customerDAL.GetCustomerServiceRequestsWithDetails(customerId);
        public void UpdateServiceRequestStatus(string requestId, string status) => _customerDAL.UpdateServiceRequestStatus(requestId, status);
        public void UpdateCustomerProfile(Customer customer) => _customerDAL.UpdateCustomerProfile(customer);
        public List<Address> GetCustomerAddresses(string customerId) => _customerDAL.GetCustomerAddresses(customerId);
        public void AddAddress(Address address) => _customerDAL.AddAddress(address);
        public void DeleteAddress(string addressId, string customerId) => _customerDAL.DeleteAddress(addressId, customerId);
        
        // Search methods
        public List<ServiceListing> SearchServices(string searchTerm, string pinCode, decimal? minPrice, decimal? maxPrice, int? minRating, DateTime? eventDate, string category = null)
            => _customerDAL.SearchServices(searchTerm, pinCode, minPrice, maxPrice, minRating, eventDate, category);
        public List<Location> GetLocations()
        {
            // Try get from cache first
            if (_cache.TryGetValue(LocationsCacheKey, out List<Location> cached))
            {
                return cached;
            }

            var locations = _customerDAL.GetLocations();

            // Cache for 10 minutes (adjust as needed)
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            _cache.Set(LocationsCacheKey, locations, cacheEntryOptions);
            return locations;
        }
        
        // Admin methods
        public Admin GetAdminByEmail(string email) => _adminDAL.GetAdminByEmail(email);
        public void RegisterAdmin(Admin admin) => _adminDAL.RegisterAdmin(admin);
        public void UpdateAdminPasswordHash(string adminId, string passwordHash) => _adminDAL.UpdateAdminPasswordHash(adminId, passwordHash);
        public int GetAdminCount() => _adminDAL.GetAdminCount();
        
        // Wallet methods
        public Customer GetCustomerById(string customerId) => _customerDAL.GetCustomerById(customerId);
        public void AddMoneyToWallet(string customerId, decimal amount, string description) 
            => _customerDAL.AddMoneyToWallet(customerId, amount, description);

        public bool RequestVendorPayout(string vendorId, decimal amount, string description)
            => _vendorDAL.RequestPayout(vendorId, amount, description);
        public List<WalletTransaction> GetWalletTransactions(string ownerId, string ownerType = "Customer", int limit = 10) 
            => _customerDAL.GetWalletTransactions(ownerId, ownerType, limit);

        // Review methods
        public void AddReview(Review review) => _reviewDAL.AddReview(review);
        public bool HasReviewForBooking(string bookingId) => _reviewDAL.HasReviewForBooking(bookingId);
        public HashSet<string> GetReviewedBookingIds(string customerId) => _reviewDAL.GetReviewedBookingIds(customerId);
        public List<Review> GetServiceReviews(string serviceId) => _reviewDAL.GetServiceReviews(serviceId);
        public List<Review> GetVendorReviews(string vendorId) => _reviewDAL.GetVendorReviews(vendorId);
        public ReviewSummary GetVendorReviewSummary(string vendorId) => _reviewDAL.GetVendorReviewSummary(vendorId);
        public Dictionary<string, ReviewSummary> GetServiceReviewSummaries(IEnumerable<string> serviceIds)
            => _reviewDAL.GetServiceReviewSummaries(serviceIds);

        // Messaging methods
        public void SendMessage(Message message) => _messageDAL.SendMessage(message);
        public List<Message> GetChatHistory(string user1Id, string user2Id) => _messageDAL.GetChatHistory(user1Id, user2Id);
        public List<MessageConversationSummary> GetUserConversations(string userId) => _messageDAL.GetConversations(userId);

        // PIN code management
        public List<AllowedPinCode> GetAllowedPinCodes() => _adminDAL.GetAllowedPinCodes();
        public void AddAllowedPinCode(string pinCode, string cityName) => _adminDAL.AddAllowedPinCode(pinCode, cityName);
        public void DeleteAllowedPinCode(string pinCode) => _adminDAL.DeleteAllowedPinCode(pinCode);
        // Serviceability is controlled at the state level: a PIN code is allowed
        // only when it resolves to a known state and that state is enabled.
        public bool IsPinCodeAllowed(string pinCode)
        {
            if (string.IsNullOrEmpty(pinCode)) return false;
            var state = _stateDAL.GetStateByPinCode(pinCode);
            if (string.IsNullOrWhiteSpace(state)) return false;
            return _stateDAL.IsStateEnabled(state);
        }

        // State management
        public List<State> GetAllStates() => _stateDAL.GetAllStates();
        public List<string> GetEnabledStateNames() => _stateDAL.GetEnabledStateNames();
        public void SetStateEnabled(int stateId, bool enabled) => _stateDAL.SetStateEnabled(stateId, enabled);
        public void SetAllStatesEnabled(bool enabled) => _stateDAL.SetAllStatesEnabled(enabled);
        public bool IsStateEnabled(string stateName) => _stateDAL.IsStateEnabled(stateName);
        public int ImportLocations(IEnumerable<Location> locations) => _stateDAL.ImportLocations(locations);

        public List<VendorScheduleEntry> GetVendorSchedule(string vendorId, DateTime fromDate, DateTime toDate)
            => _vendorDAL.GetVendorSchedule(vendorId, fromDate, toDate);

        public bool IsVendorAvailableOnDate(string vendorId, DateTime eventDate)
        {
            if (string.IsNullOrWhiteSpace(vendorId) || eventDate.Date < DateTime.Today)
            {
                return false;
            }

            var day = _vendorDAL.GetVendorSchedule(vendorId, eventDate.Date, eventDate.Date);
            return day.Any(entry => entry.Date.Date == eventDate.Date && !entry.IsBooked && !entry.IsUnderProcess);
        }

        public bool IsVendorAvailableForRange(string vendorId, DateTime startDate, DateTime endDate)
        {
            if (endDate.Date < startDate.Date) return false;
            for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
            {
                if (!IsVendorAvailableOnDate(vendorId, day)) return false;
            }
            return true;
        }

        public bool IsVendorServingPinCode(string vendorId, string partyPinCode)
            => _vendorDAL.IsVendorServingPinCode(vendorId, partyPinCode);

        public List<VendorCalendarBlock> GetVendorCalendarBlocks(string vendorId, DateTime fromDate, DateTime toDate)
            => _vendorDAL.GetVendorCalendarBlocks(vendorId, fromDate, toDate);

        public VendorCalendarBlock GetVendorCalendarBlock(int blockId, string vendorId)
            => _vendorDAL.GetVendorCalendarBlock(blockId, vendorId);

        public void AddVendorCalendarBlock(VendorCalendarBlock block)
            => _vendorDAL.AddVendorCalendarBlock(block);

        public void UpdateVendorCalendarBlock(VendorCalendarBlock block)
            => _vendorDAL.UpdateVendorCalendarBlock(block);

        public void DeleteVendorCalendarBlock(int blockId, string vendorId)
            => _vendorDAL.DeleteVendorCalendarBlock(blockId, vendorId);
    }
}
