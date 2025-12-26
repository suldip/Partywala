-- Update script to add images to existing vendors without wiping data

-- Update Ravi Kumar (Singer)
UPDATE ServiceListings 
SET MediaUrl = 'https://images.unsplash.com/photo-1516280440614-6697288d5d38?auto=format&fit=crop&q=80&w=200&h=200'
WHERE VendorId = (SELECT Id FROM Vendors WHERE Email = 'ravi.kumar@email.com' LIMIT 1);

-- Update Priya Sharma (Singer)
UPDATE ServiceListings 
SET MediaUrl = 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=200&h=200'
WHERE VendorId = (SELECT Id FROM Vendors WHERE Email = 'priya.sharma@email.com' LIMIT 1);

-- Update Magic Mike (Magician)
UPDATE ServiceListings 
SET MediaUrl = 'https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&q=80&w=200&h=200'
WHERE VendorId = (SELECT Id FROM Vendors WHERE Email = 'mike.magic@email.com' LIMIT 1);

SELECT 'Images updated successfully!' as Message;
