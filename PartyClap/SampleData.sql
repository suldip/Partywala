-- Sample Vendors and Service Listings Data for PartyClap
-- This script inserts sample vendors with realistic data matching the actual database schema

-- Note: Make sure to run this after the database is created
-- The database uses Entity Framework Code First, so tables are created automatically

-- Clear existing data (optional - comment out if you want to keep existing data)
-- DELETE FROM ServiceListings;
-- DELETE FROM Vendors;

-- Insert Sample Vendors
-- Vendor table columns: Id, Name, Email, Phone, Address, PinCode, Password, IsRegistered, TrustScore, WalletBalance

INSERT INTO Vendors (Id, Name, Email, Phone, Address, PinCode, Password, IsRegistered, TrustScore, WalletBalance)
VALUES 
-- Singers
(NEWID(), 'Ravi Kumar', 'ravi.kumar@email.com', '9876543210', 'Bandra West, Mumbai, Maharashtra', '400050', 'Password123!', 1, 100, 0),
(NEWID(), 'Priya Sharma', 'priya.sharma@email.com', '9876543211', 'Connaught Place, Delhi', '110001', 'Password123!', 1, 100, 0),

-- Magicians
(NEWID(), 'Magic Mike', 'mike.magic@email.com', '9876543212', 'MG Road, Bangalore, Karnataka', '560001', 'Password123!', 1, 100, 0),
(NEWID(), 'Rahul The Illusionist', 'rahul.illusion@email.com', '9876543213', 'Fort, Mumbai, Maharashtra', '400001', 'Password123!', 1, 100, 0),

-- Chefs & Caterers
(NEWID(), 'Chef Priya Patel', 'chef.priya@email.com', '9876543214', 'Navrangpura, Ahmedabad, Gujarat', '380001', 'Password123!', 1, 100, 0),
(NEWID(), 'Spice Masters Catering', 'info@spicemasters.com', '9876543215', 'Koregaon Park, Pune, Maharashtra', '411001', 'Password123!', 1, 100, 0),

-- Decorators
(NEWID(), 'Decorative Dreams', 'info@decorativedreams.com', '9876543216', 'Andheri West, Mumbai, Maharashtra', '400050', 'Password123!', 1, 100, 0),
(NEWID(), 'Elegant Events Decor', 'contact@elegantevents.com', '9876543217', 'Vasant Vihar, Delhi', '110016', 'Password123!', 1, 100, 0),

-- Event Managers
(NEWID(), 'Arjun Event Management', 'arjun@eventpro.com', '9876543218', 'T Nagar, Chennai, Tamil Nadu', '600001', 'Password123!', 1, 100, 0),
(NEWID(), 'Perfect Party Planners', 'info@perfectparty.com', '9876543219', 'Indiranagar, Bangalore, Karnataka', '560001', 'Password123!', 1, 100, 0),

-- Photographers
(NEWID(), 'Amit Photography', 'amit@photopro.com', '9876543220', 'Cyber City, Gurgaon, Haryana', '122001', 'Password123!', 1, 100, 0),
(NEWID(), 'Lens & Light Studios', 'contact@lenslight.com', '9876543221', 'Colaba, Mumbai, Maharashtra', '400001', 'Password123!', 1, 100, 0),

-- DJs
(NEWID(), 'DJ Rhythm', 'dj.rhythm@email.com', '9876543222', 'Banjara Hills, Hyderabad, Telangana', '500001', 'Password123!', 1, 100, 0),
(NEWID(), 'DJ Beats', 'djbeats@email.com', '9876543223', 'Viman Nagar, Pune, Maharashtra', '411001', 'Password123!', 1, 100, 0),

-- Casino Game Hosts
(NEWID(), 'Royal Casino Nights', 'info@royalcasino.com', '9876543224', 'Nariman Point, Mumbai, Maharashtra', '400001', 'Password123!', 1, 100, 0),
(NEWID(), 'Vegas Vibes', 'contact@vegasvibes.com', '9876543225', 'Whitefield, Bangalore, Karnataka', '560001', 'Password123!', 1, 100, 0);

-- Insert Service Listings
-- ServiceListing table columns: Id, VendorId, ServiceType, Description, Cost, Unit, MediaUrl, Attributes, WeekendCost

-- Get vendor IDs for reference (you'll need to update these after running the vendor inserts)
DECLARE @RaviId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'ravi.kumar@email.com');
DECLARE @PriyaId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'priya.sharma@email.com');
DECLARE @MikeId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'mike.magic@email.com');
DECLARE @RahulId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'rahul.illusion@email.com');
DECLARE @ChefPriyaId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'chef.priya@email.com');
DECLARE @SpiceMastersId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'info@spicemasters.com');
DECLARE @DecorativeDreamsId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'info@decorativedreams.com');
DECLARE @ElegantEventsId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'contact@elegantevents.com');
DECLARE @ArjunId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'arjun@eventpro.com');
DECLARE @PerfectPartyId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'info@perfectparty.com');
DECLARE @AmitId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'amit@photopro.com');
DECLARE @LensLightId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'contact@lenslight.com');
DECLARE @DJRhythmId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'dj.rhythm@email.com');
DECLARE @DJBeatsId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'djbeats@email.com');
DECLARE @RoyalCasinoId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'info@royalcasino.com');
DECLARE @VegasVibesId NVARCHAR(450) = (SELECT TOP 1 Id FROM Vendors WHERE Email = 'contact@vegasvibes.com');

INSERT INTO ServiceListings (Id, VendorId, ServiceType, Description, Cost, Unit, MediaUrl, Attributes, WeekendCost)
VALUES 
-- Singers
(NEWID(), @RaviId, 'Singer', 'Professional Bollywood and classical singer with 8+ years of experience. Specializing in wedding ceremonies and corporate events.', 5000, 'Hour', NULL, '{"specialties":["Bollywood","Classical","Weddings"],"rating":4.8,"reviews":156}', 6000),
(NEWID(), @PriyaId, 'Singer', 'Versatile singer specializing in Bollywood, Sufi, and Ghazals. Perfect for intimate gatherings and grand celebrations.', 4500, 'Hour', NULL, '{"specialties":["Bollywood","Sufi","Ghazals"],"rating":4.7,"reviews":98}', 5500),

-- Magicians
(NEWID(), @MikeId, 'Magician', 'Award-winning magician specializing in close-up magic, stage shows, and kids parties. 5+ years of making events memorable.', 8000, 'Event', NULL, '{"specialties":["Close-up Magic","Stage Shows","Kids Parties"],"rating":4.9,"reviews":89}', 10000),
(NEWID(), @RahulId, 'Magician', 'Master illusionist and mentalist. Specializing in corporate events and large-scale shows.', 12000, 'Event', NULL, '{"specialties":["Illusions","Mentalism","Corporate"],"rating":4.8,"reviews":67}', 15000),

-- Chefs & Caterers
(NEWID(), @ChefPriyaId, 'Chef', 'Expert in North Indian, South Indian, and Continental cuisine. Full catering service with cook and serve options.', 500, 'Person', NULL, '{"specialties":["North Indian","South Indian","Continental"],"rating":4.7,"reviews":203}', 600),
(NEWID(), @SpiceMastersId, 'Chef', 'Premium catering service offering multi-cuisine options. Minimum 50 people. Specializing in weddings and corporate events.', 600, 'Person', NULL, '{"specialties":["Multi-cuisine","Weddings","Corporate"],"rating":4.9,"reviews":178}', 750),

-- Decorators
(NEWID(), @DecorativeDreamsId, 'Decorator', 'Complete event decoration service. Specializing in birthday themes, wedding decor, and corporate events.', 15000, 'Event', NULL, '{"specialties":["Birthday Themes","Wedding Decor","Corporate"],"rating":4.6,"reviews":127}', 18000),
(NEWID(), @ElegantEventsId, 'Decorator', 'Premium decoration service with custom themes. Balloon art, floral arrangements, and stage setups.', 20000, 'Event', NULL, '{"specialties":["Custom Themes","Balloon Art","Floral"],"rating":4.8,"reviews":145}', 25000),

-- Event Managers
(NEWID(), @ArjunId, 'Event Manager', 'Full-service event management for corporate events, weddings, and birthday parties. End-to-end planning and execution.', 12000, 'Event', NULL, '{"specialties":["Corporate","Weddings","Birthdays"],"rating":4.5,"reviews":98}', 15000),
(NEWID(), @PerfectPartyId, 'Event Manager', 'Specialized in destination weddings and large-scale corporate events. Complete vendor coordination.', 25000, 'Event', NULL, '{"specialties":["Destination Weddings","Corporate","Coordination"],"rating":4.7,"reviews":112}', 30000),

-- Photographers
(NEWID(), @AmitId, 'Photographer', 'Professional wedding and event photographer. Candid, traditional, and contemporary styles. Quick delivery within 1 week.', 8000, 'Event', NULL, '{"specialties":["Candid","Traditional","Contemporary"],"rating":4.9,"reviews":142}', 10000),
(NEWID(), @LensLightId, 'Photographer', 'Award-winning photography studio. Specializing in pre-wedding shoots, wedding coverage, and event photography.', 15000, 'Event', NULL, '{"specialties":["Pre-wedding","Weddings","Events"],"rating":4.8,"reviews":167}', 18000),

-- DJs
(NEWID(), @DJRhythmId, 'DJ', 'Professional DJ specializing in Bollywood, EDM, and club music. Perfect for weddings and corporate parties.', 8000, 'Event', NULL, '{"specialties":["Bollywood","EDM","Club Music"],"rating":4.3,"reviews":76}', 10000),
(NEWID(), @DJBeatsId, 'DJ', 'High-energy DJ with latest equipment. Specializing in wedding receptions and college fests.', 10000, 'Event', NULL, '{"specialties":["Weddings","College Fests","High Energy"],"rating":4.6,"reviews":89}', 12000),

-- Casino Game Hosts
(NEWID(), @RoyalCasinoId, 'Casino Host', 'Professional casino game hosting with authentic equipment. Poker, Roulette, Blackjack, and more.', 25000, 'Event', NULL, '{"specialties":["Poker","Roulette","Blackjack"],"rating":4.7,"reviews":54}', 30000),
(NEWID(), @VegasVibesId, 'Casino Host', 'Complete casino party setup with professional dealers. Perfect for corporate events and private parties.', 30000, 'Event', NULL, '{"specialties":["Casino Setup","Professional Dealers","Corporate"],"rating":4.8,"reviews":43}', 35000);

-- Print success message
PRINT 'Sample data inserted successfully!';
PRINT 'Total Vendors: ' + CAST((SELECT COUNT(*) FROM Vendors) AS VARCHAR(10));
PRINT 'Total Service Listings: ' + CAST((SELECT COUNT(*) FROM ServiceListings) AS VARCHAR(10));
