using System;
using System.Collections.Generic;
using System.Linq;
using PartyClap.Models;

namespace PartyClap.Services
{
    public class InMemoryDataService : IDataService
    {
        public List<Vendor> Vendors { get; } = new List<Vendor>();
        public List<Customer> Customers { get; } = new List<Customer>();
        public List<Booking> Bookings { get; } = new List<Booking>();

        public InMemoryDataService()
        {
            // Seed some data
            Vendors.Add(new Vendor 
            { 
                Id = "v1", 
                Name = "Alice Singer", 
                Email = "alice@example.com", 
                IsRegistered = true, 
                Services = new List<ServiceListing> 
                { 
                    new ServiceListing { Id = "s1", VendorId = "v1", ServiceType = "Singer", Description = "Professional Jazz Singer", Cost = 5000, Unit = "Hour" } 
                } 
            });
            
            Customers.Add(new Customer { Id = "c1", Name = "Bob PartyHost", Email = "bob@example.com" });
        }

        public void AddVendor(Vendor vendor) => Vendors.Add(vendor);
        public void AddCustomer(Customer customer) => Customers.Add(customer);
        public void AddBooking(Booking booking) => Bookings.Add(booking);
        
        public void UpdateVendor(Vendor vendor)
        {
            var existing = Vendors.FirstOrDefault(v => v.Id == vendor.Id);
            if (existing != null)
            {
                Vendors.Remove(existing);
                Vendors.Add(vendor);
            }
        }

        public Vendor GetVendor(string id) => Vendors.FirstOrDefault(v => v.Id == id);
        public Customer GetCustomer(string id) => Customers.FirstOrDefault(c => c.Id == id);
        
        // Service methods
        public void AddService(ServiceListing service) => throw new NotImplementedException();
        public List<ServiceListing> GetVendorServices(string vendorId) => throw new NotImplementedException();
        public ServiceListing GetService(string serviceId) => throw new NotImplementedException();
        
        // Booking methods
        public List<Booking> GetVendorBookings(string vendorId) => throw new NotImplementedException();
        public void UpdateBookingStatus(string bookingId, string status, bool balancePaidOnApp) => throw new NotImplementedException();
        public void UpdateBookingStatus(string bookingId, string status, decimal? vendorCost = null, decimal? customerTotalCost = null) => throw new NotImplementedException();
        public void MarkBalanceAsPaid(string bookingId) => throw new NotImplementedException();
        
        // Portfolio methods
        public void AddPortfolioItem(PortfolioItem item) => throw new NotImplementedException();
        public List<PortfolioItem> GetVendorPortfolio(string vendorId) => throw new NotImplementedException();
        
        // Cart methods
        public void AddToCart(string cookieId, string serviceId, string vendorId, DateTime? eventDate) => throw new NotImplementedException();
        public List<CartItem> GetCartItems(string cookieId) => throw new NotImplementedException();
        public void RemoveFromCart(int cartItemId) => throw new NotImplementedException();
        public void ClearCart(string cookieId) => throw new NotImplementedException();
        public void UpdateCartItemDate(int cartItemId, DateTime? eventDate) => throw new NotImplementedException();
        
        // Search methods
        public List<ServiceListing> SearchServices(string searchTerm, string pinCode, decimal? minPrice, decimal? maxPrice, int? minRating, DateTime? eventDate) => throw new NotImplementedException();
        public List<Location> GetLocations() => throw new NotImplementedException();
        
        // Customer methods
        public void RegisterCustomer(Customer customer) => Customers.Add(customer);
        public List<Booking> GetCustomerBookings(string customerId) => throw new NotImplementedException();
        public void CreateServiceRequest(ServiceRequest request) => throw new NotImplementedException();
        public List<ServiceRequest> GetVendorServiceRequests(string vendorId) => throw new NotImplementedException();
        public List<Dictionary<string, object>> GetVendorServiceRequestsWithDetails(string vendorId) => throw new NotImplementedException();
        public List<Dictionary<string, object>> GetCustomerServiceRequestsWithDetails(string customerId) => throw new NotImplementedException();
        public void UpdateServiceRequestStatus(string requestId, string status) => throw new NotImplementedException();
        public Customer GetCustomerByEmail(string email) => Customers.FirstOrDefault(c => c.Email == email);
        
        // Admin methods
        public Admin GetAdminByEmail(string email) => throw new NotImplementedException();
        public void RegisterAdmin(Admin admin) => throw new NotImplementedException();
        
        // Vendor Auth
        public Vendor GetVendorByEmail(string email) => Vendors.FirstOrDefault(v => v.Email == email);

        public Booking GetBooking(string bookingId)
        {
            throw new NotImplementedException();
        }

        public Vendor GetVendorByPhone(string phone) => Vendors.FirstOrDefault(v => v.Phone == phone);
        public Customer GetCustomerByPhone(string phone) => Customers.FirstOrDefault(c => c.Phone == phone);
        
        // Wallet methods
        public Customer GetCustomerById(string customerId) => Customers.FirstOrDefault(c => c.Id == customerId);
        public void AddMoneyToWallet(string customerId, decimal amount, string description) => throw new NotImplementedException();
        public List<WalletTransaction> GetWalletTransactions(string customerId, int limit = 10) => throw new NotImplementedException();

        // Review methods
        public void AddReview(Review review) => throw new NotImplementedException();
        public List<Review> GetServiceReviews(string serviceId) => throw new NotImplementedException();
        public List<Review> GetVendorReviews(string vendorId) => throw new NotImplementedException();
        public List<Message> GetChatHistory(string user1Id, string user2Id) => throw new NotImplementedException();
        public void SendMessage(Message message) => throw new NotImplementedException();
    }
}
