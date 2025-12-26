using System;
using System.Collections.Generic;
using PartyClap.Models;
using PartyClap.DAL;

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

        public AdoNetDataService(VendorDAL vendorDAL, CustomerDAL customerDAL, AdminDAL adminDAL, CartDAL cartDAL, ReviewDAL reviewDAL, MessageDAL messageDAL)
        {
            _vendorDAL = vendorDAL;
            _customerDAL = customerDAL;
            _adminDAL = adminDAL;
            _cartDAL = cartDAL;
            _reviewDAL = reviewDAL;
            _messageDAL = messageDAL;
        }

        // Mock properties for backward compatibility
        public List<Vendor> Vendors => throw new NotImplementedException();
        public List<Customer> Customers => throw new NotImplementedException();
        public List<Booking> Bookings => throw new NotImplementedException();

        // Vendor methods
        public void AddVendor(Vendor vendor) => _vendorDAL.RegisterVendor(vendor);
        public Vendor GetVendor(string id) => _vendorDAL.GetVendor(id);
        public Vendor GetVendorByEmail(string email) => _vendorDAL.GetVendorByEmail(email);
        public Vendor GetVendorByPhone(string phone) => _vendorDAL.GetVendorByPhone(phone);
        public void UpdateVendor(Vendor vendor) => _vendorDAL.UpdateVendor(vendor);
        
        // Service methods
        public void AddService(ServiceListing service) => _vendorDAL.AddService(service);
        public List<ServiceListing> GetVendorServices(string vendorId) => _vendorDAL.GetVendorServices(vendorId);
        public ServiceListing GetService(string serviceId) => _vendorDAL.GetService(serviceId);
        
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
        public void MarkBalanceAsPaid(string bookingId) => _vendorDAL.MarkBalanceAsPaid(bookingId);
        
        // Cart methods
        public void AddToCart(string cookieId, string serviceId, string vendorId, DateTime? eventDate) 
            => _cartDAL.AddToCart(cookieId, serviceId, vendorId, eventDate);
        public List<CartItem> GetCartItems(string cookieId) => _cartDAL.GetCartItems(cookieId);
        public void RemoveFromCart(int cartItemId) => _cartDAL.RemoveFromCart(cartItemId);
        public void ClearCart(string cookieId) => _cartDAL.ClearCart(cookieId);
        public void UpdateCartItemDate(int cartItemId, DateTime? eventDate) => _cartDAL.UpdateCartItemDate(cartItemId, eventDate);
        
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
        
        // Search methods
        public List<ServiceListing> SearchServices(string searchTerm, string pinCode, decimal? minPrice, decimal? maxPrice, int? minRating, DateTime? eventDate)
            => _customerDAL.SearchServices(searchTerm, pinCode, minPrice, maxPrice, minRating, eventDate);
        public List<Location> GetLocations() => _customerDAL.GetLocations();
        
        // Admin methods
        public Admin GetAdminByEmail(string email) => _adminDAL.GetAdminByEmail(email);
        public void RegisterAdmin(Admin admin) => _adminDAL.RegisterAdmin(admin);
        
        // Wallet methods
        public Customer GetCustomerById(string customerId) => _customerDAL.GetCustomerById(customerId);
        public void AddMoneyToWallet(string customerId, decimal amount, string description) 
            => _customerDAL.AddMoneyToWallet(customerId, amount, description);
        public List<WalletTransaction> GetWalletTransactions(string customerId, int limit = 10) 
            => _customerDAL.GetWalletTransactions(customerId, limit);

        // Review methods
        public void AddReview(Review review) => _reviewDAL.AddReview(review);
        public List<Review> GetServiceReviews(string serviceId) => _reviewDAL.GetServiceReviews(serviceId);
        public List<Review> GetVendorReviews(string vendorId) => _reviewDAL.GetVendorReviews(vendorId);

        // Messaging methods
        public void SendMessage(Message message) => _messageDAL.SendMessage(message);
        public List<Message> GetChatHistory(string user1Id, string user2Id) => _messageDAL.GetChatHistory(user1Id, user2Id);
    }
}
