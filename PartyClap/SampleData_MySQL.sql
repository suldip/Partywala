-- Sample Vendors and Service Listings Data for PartyClap (MySQL - Corrected)
-- This script inserts sample vendors matching the actual database schema

-- Ensure Locations exist
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES 
('110016', 'Vasant Vihar', 'New Delhi', 'Delhi'),
('122001', 'Cyber City', 'Gurgaon', 'Haryana'),
('380001', 'Navrangpura', 'Ahmedabad', 'Gujarat'),
('411001', 'Pune City', 'Pune', 'Maharashtra'),
('500001', 'Banjara Hills', 'Hyderabad', 'Telangana'),
('600001', 'Chennai City', 'Chennai', 'Tamil Nadu');

-- Insert Sample Customer
INSERT IGNORE INTO Customers (Id, Name, Email, Phone, PasswordHash)
VALUES ('test_customer_1', 'Test Customer', 'test@customer.com', '9999999999', 'hashed_pw_placeholder');

-- Insert Sample Vendors (without Password column)
INSERT IGNORE INTO Vendors (Id, Name, Email, Phone, Address, PinCode, IsRegistered, TrustScore, WalletBalance)
VALUES 
-- Singers
(UUID(), 'Ravi Kumar', 'ravi.kumar@email.com', '9876543210', 'Bandra West, Mumbai, Maharashtra', '400050', 1, 100, 0),
(UUID(), 'Priya Sharma', 'priya.sharma@email.com', '9876543211', 'Connaught Place, Delhi', '110001', 1, 100, 0),

-- Magicians
(UUID(), 'Magic Mike', 'mike.magic@email.com', '9876543212', 'MG Road, Bangalore, Karnataka', '560001', 1, 100, 0),
(UUID(), 'Rahul The Illusionist', 'rahul.illusion@email.com', '9876543213', 'Fort, Mumbai, Maharashtra', '400001', 1, 100, 0),

-- Chefs & Caterers
(UUID(), 'Chef Priya Patel', 'chef.priya@email.com', '9876543214', 'Navrangpura, Ahmedabad, Gujarat', '380001', 1, 100, 0),
(UUID(), 'Spice Masters Catering', 'info@spicemasters.com', '9876543215', 'Koregaon Park, Pune, Maharashtra', '411001', 1, 100, 0),

-- Decorators
(UUID(), 'Decorative Dreams', 'info@decorativedreams.com', '9876543216', 'Andheri West, Mumbai, Maharashtra', '400050', 1, 100, 0),
(UUID(), 'Elegant Events Decor', 'contact@elegantevents.com', '9876543217', 'Vasant Vihar, Delhi', '110016', 1, 100, 0),

-- Event Managers
(UUID(), 'Arjun Event Management', 'arjun@eventpro.com', '9876543218', 'T Nagar, Chennai, Tamil Nadu', '600001', 1, 100, 0),
(UUID(), 'Perfect Party Planners', 'info@perfectparty.com', '9876543219', 'Indiranagar, Bangalore, Karnataka', '560001', 1, 100, 0),

-- Photographers
(UUID(), 'Amit Photography', 'amit@photopro.com', '9876543220', 'Cyber City, Gurgaon, Haryana', '122001', 1, 100, 0),
(UUID(), 'Lens & Light Studios', 'contact@lenslight.com', '9876543221', 'Colaba, Mumbai, Maharashtra', '400001', 1, 100, 0),

-- DJs
(UUID(), 'DJ Rhythm', 'dj.rhythm@email.com', '9876543222', 'Banjara Hills, Hyderabad, Telangana', '500001', 1, 100, 0),
(UUID(), 'DJ Beats', 'djbeats@email.com', '9876543223', 'Viman Nagar, Pune, Maharashtra', '411001', 1, 100, 0),

-- Casino Game Hosts
(UUID(), 'Royal Casino Nights', 'info@royalcasino.com', '9876543224', 'Nariman Point, Mumbai, Maharashtra', '400001', 1, 100, 0),
(UUID(), 'Vegas Vibes', 'contact@vegasvibes.com', '9876543225', 'Whitefield, Bangalore, Karnataka', '560001', 1, 100, 0);

-- Get vendor IDs
SET @RaviId = (SELECT Id FROM Vendors WHERE Email = 'ravi.kumar@email.com' LIMIT 1);
SET @PriyaId = (SELECT Id FROM Vendors WHERE Email = 'priya.sharma@email.com' LIMIT 1);
SET @MikeId = (SELECT Id FROM Vendors WHERE Email = 'mike.magic@email.com' LIMIT 1);
SET @RahulId = (SELECT Id FROM Vendors WHERE Email = 'rahul.illusion@email.com' LIMIT 1);
SET @ChefPriyaId = (SELECT Id FROM Vendors WHERE Email = 'chef.priya@email.com' LIMIT 1);
SET @SpiceMastersId = (SELECT Id FROM Vendors WHERE Email = 'info@spicemasters.com' LIMIT 1);
SET @DecorativeDreamsId = (SELECT Id FROM Vendors WHERE Email = 'info@decorativedreams.com' LIMIT 1);
SET @ElegantEventsId = (SELECT Id FROM Vendors WHERE Email = 'contact@elegantevents.com' LIMIT 1);
SET @ArjunId = (SELECT Id FROM Vendors WHERE Email = 'arjun@eventpro.com' LIMIT 1);
SET @PerfectPartyId = (SELECT Id FROM Vendors WHERE Email = 'info@perfectparty.com' LIMIT 1);
SET @AmitId = (SELECT Id FROM Vendors WHERE Email = 'amit@photopro.com' LIMIT 1);
SET @LensLightId = (SELECT Id FROM Vendors WHERE Email = 'contact@lenslight.com' LIMIT 1);
SET @DJRhythmId = (SELECT Id FROM Vendors WHERE Email = 'dj.rhythm@email.com' LIMIT 1);
SET @DJBeatsId = (SELECT Id FROM Vendors WHERE Email = 'djbeats@email.com' LIMIT 1);
SET @RoyalCasinoId = (SELECT Id FROM Vendors WHERE Email = 'info@royalcasino.com' LIMIT 1);
SET @VegasVibesId = (SELECT Id FROM Vendors WHERE Email = 'contact@vegasvibes.com' LIMIT 1);

-- Insert Service Listings
INSERT IGNORE INTO Services (Id, VendorId, ServiceType, Description, Cost, Unit, MediaUrl, Attributes, WeekendCost)
VALUES 
-- Singers
(UUID(), @RaviId, 'Singer', 'Professional Bollywood and classical singer with 8+ years of experience. Specializing in wedding ceremonies and corporate events.', 5000, 'Hour', 'https://images.unsplash.com/photo-1516280440614-6697288d5d38?auto=format&fit=crop&q=80&w=200&h=200', '{"specialties":["Bollywood","Classical","Weddings"],"rating":4.8,"reviews":156}', 6000),
(UUID(), @PriyaId, 'Singer', 'Versatile singer specializing in Bollywood, Sufi, and Ghazals. Perfect for intimate gatherings and grand celebrations.', 4500, 'Hour', 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=200&h=200', '{"specialties":["Bollywood","Sufi","Ghazals"],"rating":4.7,"reviews":98}', 5500),

-- Magicians
(UUID(), @MikeId, 'Magician', 'Award-winning magician specializing in close-up magic, stage shows, and kids parties. 5+ years of making events memorable.', 8000, 'Event', 'https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&q=80&w=200&h=200', '{"specialties":["Close-up Magic","Stage Shows","Kids Parties"],"rating":4.9,"reviews":89}', 10000),
(UUID(), @RahulId, 'Magician', 'Master illusionist and mentalist. Specializing in corporate events and large-scale shows.', 12000, 'Event', NULL, '{"specialties":["Illusions","Mentalism","Corporate"],"rating":4.8,"reviews":67}', 15000),

-- Chefs & Caterers
(UUID(), @ChefPriyaId, 'Chef', 'Expert in North Indian, South Indian, and Continental cuisine. Full catering service with cook and serve options.', 500, 'Person', NULL, '{"specialties":["North Indian","South Indian","Continental"],"rating":4.7,"reviews":203}', 600),
(UUID(), @SpiceMastersId, 'Chef', 'Premium catering service offering multi-cuisine options. Minimum 50 people. Specializing in weddings and corporate events.', 600, 'Person', NULL, '{"specialties":["Multi-cuisine","Weddings","Corporate"],"rating":4.9,"reviews":178}', 750),

-- Decorators
(UUID(), @DecorativeDreamsId, 'Decorator', 'Complete event decoration service. Specializing in birthday themes, wedding decor, and corporate events.', 15000, 'Event', NULL, '{"specialties":["Birthday Themes","Wedding Decor","Corporate"],"rating":4.6,"reviews":127}', 18000),
(UUID(), @ElegantEventsId, 'Decorator', 'Premium decoration service with custom themes. Balloon art, floral arrangements, and stage setups.', 20000, 'Event', NULL, '{"specialties":["Custom Themes","Balloon Art","Floral"],"rating":4.8,"reviews":145}', 25000),

-- Event Managers
(UUID(), @ArjunId, 'Event Manager', 'Full-service event management for corporate events, weddings, and birthday parties. End-to-end planning and execution.', 12000, 'Event', NULL, '{"specialties":["Corporate","Weddings","Birthdays"],"rating":4.5,"reviews":98}', 15000),
(UUID(), @PerfectPartyId, 'Event Manager', 'Specialized in destination weddings and large-scale corporate events. Complete vendor coordination.', 25000, 'Event', NULL, '{"specialties":["Destination Weddings","Corporate","Coordination"],"rating":4.7,"reviews":112}', 30000),

-- Photographers
(UUID(), @AmitId, 'Photographer', 'Professional wedding and event photographer. Candid, traditional, and contemporary styles. Quick delivery within 1 week.', 8000, 'Event', NULL, '{"specialties":["Candid","Traditional","Contemporary"],"rating":4.9,"reviews":142}', 10000),
(UUID(), @LensLightId, 'Photographer', 'Award-winning photography studio. Specializing in pre-wedding shoots, wedding coverage, and event photography.', 15000, 'Event', NULL, '{"specialties":["Pre-wedding","Weddings","Events"],"rating":4.8,"reviews":167}', 18000),

-- DJs
(UUID(), @DJRhythmId, 'DJ', 'Professional DJ specializing in Bollywood, EDM, and club music. Perfect for weddings and corporate parties.', 8000, 'Event', NULL, '{"specialties":["Bollywood","EDM","Club Music"],"rating":4.3,"reviews":76}', 10000),
(UUID(), @DJBeatsId, 'DJ', 'High-energy DJ with latest equipment. Specializing in wedding receptions and college fests.', 10000, 'Event', NULL, '{"specialties":["Weddings","College Fests","High Energy"],"rating":4.6,"reviews":89}', 12000),

-- Casino Game Hosts
(UUID(), @RoyalCasinoId, 'Casino Host', 'Professional casino game hosting with authentic equipment. Poker, Roulette, Blackjack, and more.', 25000, 'Event', NULL, '{"specialties":["Poker","Roulette","Blackjack"],"rating":4.7,"reviews":54}', 30000),
(UUID(), @VegasVibesId, 'Casino Host', 'Complete casino party setup with professional dealers. Perfect for corporate events and private parties.', 30000, 'Event', NULL, '{"specialties":["Casino Setup","Professional Dealers","Corporate"],"rating":4.8,"reviews":43}', 35000);

-- Success message
SELECT 'Sample data inserted successfully!' AS Message;
SELECT CONCAT('Total Vendors: ', COUNT(*)) AS VendorCount FROM Vendors;
SELECT CONCAT('Total Service Listings: ', COUNT(*)) AS ListingCount FROM Services;
