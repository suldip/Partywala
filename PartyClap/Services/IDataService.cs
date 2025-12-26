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
        void AddCustomer(Customer customer);
        void AddBooking(Booking booking);
        void UpdateVendor(Vendor vendor);
        Vendor GetVendor(string id);
        Customer GetCustomer(string id);
        
        // Service methods
        void AddService(ServiceListing service);
        List<ServiceListing> GetVendorServices(string vendorId);
        ServiceListing GetService(string serviceId);
        
        // Booking methods
        List<Booking> GetVendorBookings(string vendorId);
        Booking GetBooking(string bookingId);
        void UpdateBookingStatus(string bookingId, string status, bool balancePaidOnApp);
        void UpdateBookingStatus(string bookingId, string status, decimal? vendorCost = null, decimal? customerTotalCost = null);
        void MarkBalanceAsPaid(string bookingId);
        
        // Portfolio methods
        void AddPortfolioItem(PortfolioItem item);
        List<PortfolioItem> GetVendorPortfolio(string vendorId);
        
        // Cart Methods
        void AddToCart(string cookieId, string serviceId, string vendorId, DateTime? eventDate);
        List<CartItem> GetCartItems(string cookieId);
        void RemoveFromCart(int cartItemId);
        void ClearCart(string cookieId);
        void UpdateCartItemDate(int cartItemId, DateTime? eventDate);
        
        // Search methods
        List<ServiceListing> SearchServices(string searchTerm, string pinCode, decimal? minPrice, decimal? maxPrice, int? minRating, DateTime? eventDate);
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
        
        // Admin methods
        Admin GetAdminByEmail(string email);
        void RegisterAdmin(Admin admin);
        
        // Vendor Auth
        Vendor GetVendorByEmail(string email);
        Vendor GetVendorByPhone(string phone);
        Customer GetCustomerByPhone(string phone);
        
        // Wallet methods
        Customer GetCustomerById(string customerId);
        void AddMoneyToWallet(string customerId, decimal amount, string description);
        List<WalletTransaction> GetWalletTransactions(string customerId, int limit = 10);
        
        // Review methods
        void AddReview(Review review);
        List<Review> GetServiceReviews(string serviceId);
        List<Review> GetVendorReviews(string vendorId);

        // Messaging methods
        void SendMessage(Message message);
        List<Message> GetChatHistory(string user1Id, string user2Id);
    }
}
