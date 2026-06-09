using System;
using System.Collections.Generic;
using PartyClap.Models;

namespace PartyClap.Services
{
    public interface IDataService
    {
        List<Vendor> Vendors { get; }
        List<Customer> Customers { get; }
        List<Booking> Bookings { get; }
        
        void AddVendor(Vendor vendor);
        List<Vendor> GetAllVendors();
        List<AdminVendorCalendarSummary> GetVendorCalendarSummaries(DateTime fromDate, DateTime toDate);
        void AddCustomer(Customer customer);
        void AddBooking(Booking booking);
        void UpdateVendor(Vendor vendor);
        Vendor GetVendor(string id);
        Customer GetCustomer(string id);
        
        // Service methods
        void AddService(ServiceListing service);
        List<ServiceListing> GetVendorServices(string vendorId);
        ServiceListing GetService(string serviceId);
        void UpdateService(ServiceListing service);
        void DeleteService(string serviceId);
        
        // Booking methods
        List<Booking> GetVendorBookings(string vendorId);
        Booking GetBooking(string bookingId);
        void UpdateBookingStatus(string bookingId, string status, bool balancePaidOnApp);
        void UpdateBookingStatus(string bookingId, string status, decimal? vendorCost = null, decimal? customerTotalCost = null);
        void MarkAdvanceAsPaid(string bookingId);
        void MarkBalanceAsPaid(string bookingId);
        
        // Portfolio methods
        void AddPortfolioItem(PortfolioItem item);
        List<PortfolioItem> GetVendorPortfolio(string vendorId);
        
        // Cart Methods
        void AddToCart(string customerId, string cookieId, string serviceId, string vendorId, DateTime? eventDate,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null);
        List<CartItem> GetCartItems(string customerId, string cookieId);
        void MergeGuestCart(string customerId, string cookieId);
        void RemoveFromCart(int cartItemId);
        void ClearCart(string customerId, string cookieId);
        void UpdateCartItemDate(int cartItemId, DateTime? eventDate);
        void UpdateCartItemSchedule(int cartItemId, DateTime? eventDate, DateTime? eventEndDate, string startTime, string endTime,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null);
        void UpdateBookingSchedule(string bookingId, string startTime, string endTime, DateTime? eventEndDate = null,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null);
        
        // Search methods
        List<ServiceListing> SearchServices(string searchTerm, string pinCode, decimal? minPrice, decimal? maxPrice, int? minRating, DateTime? eventDate, string category = null);
        List<Location> GetLocations();
        
        // Customer methods
        void RegisterCustomer(Customer customer);
        List<Booking> GetCustomerBookings(string customerId);
        Customer GetCustomerByEmail(string email);
        void CreateServiceRequest(ServiceRequest request);
        List<ServiceRequest> GetVendorServiceRequests(string vendorId);
        List<Dictionary<string, object>> GetVendorServiceRequestsWithDetails(string vendorId);
        List<Dictionary<string, object>> GetCustomerServiceRequestsWithDetails(string customerId);
        void UpdateServiceRequestStatus(string requestId, string status);
        void UpdateCustomerProfile(Customer customer);
        List<Address> GetCustomerAddresses(string customerId);
        void AddAddress(Address address);
        void DeleteAddress(string addressId, string customerId);
        
        // Admin methods
        Admin GetAdminByEmail(string email);
        void RegisterAdmin(Admin admin);
        void UpdateAdminPasswordHash(string adminId, string passwordHash);
        int GetAdminCount();
        
        // Vendor Auth
        Vendor GetVendorByEmail(string email);
        Vendor GetVendorByPhone(string phone);
        Customer GetCustomerByPhone(string phone);
        
        // Wallet methods
        Customer GetCustomerById(string customerId);
        void AddMoneyToWallet(string customerId, decimal amount, string description);
        bool RequestVendorPayout(string vendorId, decimal amount, string description);
        List<WalletTransaction> GetWalletTransactions(string ownerId, string ownerType = "Customer", int limit = 10);
        
        // Review methods
        void AddReview(Review review);
        bool HasReviewForBooking(string bookingId);
        HashSet<string> GetReviewedBookingIds(string customerId);
        List<Review> GetServiceReviews(string serviceId);
        List<Review> GetVendorReviews(string vendorId);
        ReviewSummary GetVendorReviewSummary(string vendorId);
        Dictionary<string, ReviewSummary> GetServiceReviewSummaries(IEnumerable<string> serviceIds);

        // Messaging methods
        void SendMessage(Message message);
        List<Message> GetChatHistory(string user1Id, string user2Id);
        List<MessageConversationSummary> GetUserConversations(string userId);
        // PIN code management
        List<AllowedPinCode> GetAllowedPinCodes();
        void AddAllowedPinCode(string pinCode, string cityName);
        void DeleteAllowedPinCode(string pinCode);
        bool IsPinCodeAllowed(string pinCode);

        // State management (serviceable-area control)
        List<State> GetAllStates();
        List<string> GetEnabledStateNames();
        void SetStateEnabled(int stateId, bool enabled);
        void SetAllStatesEnabled(bool enabled);
        bool IsStateEnabled(string stateName);
        int ImportLocations(IEnumerable<Location> locations);

        List<VendorScheduleEntry> GetVendorSchedule(string vendorId, DateTime fromDate, DateTime toDate);
        bool IsVendorAvailableOnDate(string vendorId, DateTime eventDate);
        bool IsVendorAvailableForRange(string vendorId, DateTime startDate, DateTime endDate);
        bool IsVendorServingPinCode(string vendorId, string partyPinCode);
        List<VendorCalendarBlock> GetVendorCalendarBlocks(string vendorId, DateTime fromDate, DateTime toDate);
        VendorCalendarBlock GetVendorCalendarBlock(int blockId, string vendorId);
        void AddVendorCalendarBlock(VendorCalendarBlock block);
        void UpdateVendorCalendarBlock(VendorCalendarBlock block);
        void DeleteVendorCalendarBlock(int blockId, string vendorId);
    }
}
