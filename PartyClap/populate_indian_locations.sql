-- Script to populate Locations table with Indian States, Cities, and Pin Codes
-- This is a comprehensive list of major Indian locations
-- Note: For a complete database, you would need to import a full Indian pincode database
-- This script includes major cities and their pin codes

USE PartyClapDB;

-- Clear existing data (optional - comment out if you want to keep existing data)
-- DELETE FROM Locations;

-- Insert Indian States, Cities, and Pin Codes
-- Format: PinCode, AreaName, City, State

-- Maharashtra
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('400001', 'Fort', 'Mumbai', 'Maharashtra'),
('400050', 'Bandra West', 'Mumbai', 'Maharashtra'),
('400070', 'Andheri West', 'Mumbai', 'Maharashtra'),
('411001', 'Pune City', 'Pune', 'Maharashtra'),
('411002', 'Shivajinagar', 'Pune', 'Maharashtra'),
('440001', 'Civil Lines', 'Nagpur', 'Maharashtra'),
('422001', 'Nashik Road', 'Nashik', 'Maharashtra'),
('431001', 'Aurangabad City', 'Aurangabad', 'Maharashtra'),
('400601', 'Thane West', 'Thane', 'Maharashtra'),
('413001', 'Solapur City', 'Solapur', 'Maharashtra');

-- Delhi
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('110001', 'Connaught Place', 'New Delhi', 'Delhi'),
('110002', 'Daryaganj', 'New Delhi', 'Delhi'),
('110007', 'Civil Lines', 'New Delhi', 'Delhi'),
('110017', 'Saket', 'New Delhi', 'Delhi'),
('110020', 'Hauz Khas', 'New Delhi', 'Delhi'),
('110031', 'Preet Vihar', 'New Delhi', 'Delhi'),
('110015', 'Dwarka', 'New Delhi', 'Delhi');

-- Karnataka
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('560001', 'MG Road', 'Bangalore', 'Karnataka'),
('560038', 'Indiranagar', 'Bangalore', 'Karnataka'),
('560025', 'Koramangala', 'Bangalore', 'Karnataka'),
('570001', 'Mysore City', 'Mysore', 'Karnataka'),
('575001', 'Mangalore City', 'Mangalore', 'Karnataka'),
('580020', 'Hubli City', 'Hubli', 'Karnataka'),
('590001', 'Belgaum City', 'Belgaum', 'Karnataka'),
('585101', 'Gulbarga City', 'Gulbarga', 'Karnataka');

-- Tamil Nadu
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('600001', 'Parrys', 'Chennai', 'Tamil Nadu'),
('600002', 'Mount Road', 'Chennai', 'Tamil Nadu'),
('600034', 'Anna Nagar', 'Chennai', 'Tamil Nadu'),
('641001', 'Coimbatore City', 'Coimbatore', 'Tamil Nadu'),
('625001', 'Madurai City', 'Madurai', 'Tamil Nadu'),
('620001', 'Tiruchirappalli City', 'Tiruchirappalli', 'Tamil Nadu'),
('636001', 'Salem City', 'Salem', 'Tamil Nadu'),
('627001', 'Tirunelveli City', 'Tirunelveli', 'Tamil Nadu');

-- Gujarat
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('380001', 'Ahmedabad City', 'Ahmedabad', 'Gujarat'),
('380006', 'Navrangpura', 'Ahmedabad', 'Gujarat'),
('395001', 'Surat City', 'Surat', 'Gujarat'),
('390001', 'Vadodara City', 'Vadodara', 'Gujarat'),
('360001', 'Rajkot City', 'Rajkot', 'Gujarat'),
('364001', 'Bhavnagar City', 'Bhavnagar', 'Gujarat'),
('361001', 'Jamnagar City', 'Jamnagar', 'Gujarat');

-- Rajasthan
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('302001', 'Jaipur City', 'Jaipur', 'Rajasthan'),
('302002', 'C-Scheme', 'Jaipur', 'Rajasthan'),
('342001', 'Jodhpur City', 'Jodhpur', 'Rajasthan'),
('313001', 'Udaipur City', 'Udaipur', 'Rajasthan'),
('324001', 'Kota City', 'Kota', 'Rajasthan'),
('305001', 'Ajmer City', 'Ajmer', 'Rajasthan'),
('334001', 'Bikaner City', 'Bikaner', 'Rajasthan');

-- Uttar Pradesh
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('226001', 'Lucknow City', 'Lucknow', 'Uttar Pradesh'),
('208001', 'Kanpur City', 'Kanpur', 'Uttar Pradesh'),
('201001', 'Ghaziabad City', 'Ghaziabad', 'Uttar Pradesh'),
('282001', 'Agra City', 'Agra', 'Uttar Pradesh'),
('221001', 'Varanasi City', 'Varanasi', 'Uttar Pradesh'),
('250001', 'Meerut City', 'Meerut', 'Uttar Pradesh'),
('243001', 'Bareilly City', 'Bareilly', 'Uttar Pradesh');

-- West Bengal
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('700001', 'Kolkata GPO', 'Kolkata', 'West Bengal'),
('700019', 'Park Street', 'Kolkata', 'West Bengal'),
('700091', 'Salt Lake', 'Kolkata', 'West Bengal'),
('711101', 'Howrah City', 'Howrah', 'West Bengal'),
('713201', 'Durgapur City', 'Durgapur', 'West Bengal'),
('713301', 'Asansol City', 'Asansol', 'West Bengal'),
('734001', 'Siliguri City', 'Siliguri', 'West Bengal');

-- Telangana
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('500001', 'Hyderabad GPO', 'Hyderabad', 'Telangana'),
('500032', 'Banjara Hills', 'Hyderabad', 'Telangana'),
('500082', 'Hitech City', 'Hyderabad', 'Telangana'),
('506001', 'Warangal City', 'Warangal', 'Telangana'),
('503001', 'Nizamabad City', 'Nizamabad', 'Telangana'),
('505001', 'Karimnagar City', 'Karimnagar', 'Telangana'),
('507001', 'Khammam City', 'Khammam', 'Telangana');

-- Andhra Pradesh
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('530001', 'Visakhapatnam City', 'Visakhapatnam', 'Andhra Pradesh'),
('520001', 'Vijayawada City', 'Vijayawada', 'Andhra Pradesh'),
('522001', 'Guntur City', 'Guntur', 'Andhra Pradesh'),
('524001', 'Nellore City', 'Nellore', 'Andhra Pradesh'),
('518001', 'Kurnool City', 'Kurnool', 'Andhra Pradesh'),
('515001', 'Anantapur City', 'Anantapur', 'Andhra Pradesh');

-- Kerala
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('695001', 'Thiruvananthapuram City', 'Thiruvananthapuram', 'Kerala'),
('682001', 'Kochi City', 'Kochi', 'Kerala'),
('673001', 'Kozhikode City', 'Kozhikode', 'Kerala'),
('680001', 'Thrissur City', 'Thrissur', 'Kerala'),
('691001', 'Kollam City', 'Kollam', 'Kerala'),
('686001', 'Kottayam City', 'Kottayam', 'Kerala');

-- Madhya Pradesh
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('462001', 'Bhopal City', 'Bhopal', 'Madhya Pradesh'),
('452001', 'Indore City', 'Indore', 'Madhya Pradesh'),
('474001', 'Gwalior City', 'Gwalior', 'Madhya Pradesh'),
('482001', 'Jabalpur City', 'Jabalpur', 'Madhya Pradesh'),
('456001', 'Ujjain City', 'Ujjain', 'Madhya Pradesh'),
('455001', 'Dewas City', 'Dewas', 'Madhya Pradesh');

-- Punjab
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('160001', 'Chandigarh Sector 17', 'Chandigarh', 'Punjab'),
('141001', 'Ludhiana City', 'Ludhiana', 'Punjab'),
('143001', 'Amritsar City', 'Amritsar', 'Punjab'),
('144001', 'Jalandhar City', 'Jalandhar', 'Punjab'),
('147001', 'Patiala City', 'Patiala', 'Punjab'),
('140401', 'Mohali City', 'Mohali', 'Punjab');

-- Haryana
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('122001', 'Gurugram Sector 14', 'Gurugram', 'Haryana'),
('121001', 'Faridabad Sector 15', 'Faridabad', 'Haryana'),
('132103', 'Panipat City', 'Panipat', 'Haryana'),
('133001', 'Ambala City', 'Ambala', 'Haryana'),
('132001', 'Karnal City', 'Karnal', 'Haryana'),
('124001', 'Rohtak City', 'Rohtak', 'Haryana');

-- Bihar
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('800001', 'Patna City', 'Patna', 'Bihar'),
('823001', 'Gaya City', 'Gaya', 'Bihar'),
('812001', 'Bhagalpur City', 'Bhagalpur', 'Bihar'),
('842001', 'Muzaffarpur City', 'Muzaffarpur', 'Bihar'),
('846004', 'Darbhanga City', 'Darbhanga', 'Bihar'),
('851101', 'Begusarai City', 'Begusarai', 'Bihar');

-- Odisha
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('751001', 'Bhubaneswar City', 'Bhubaneswar', 'Odisha'),
('753001', 'Cuttack City', 'Cuttack', 'Odisha'),
('751002', 'Bhubaneswar Sector 1', 'Bhubaneswar', 'Odisha');

-- Assam
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('781001', 'Guwahati City', 'Guwahati', 'Assam'),
('785001', 'Jorhat City', 'Jorhat', 'Assam'),
('786001', 'Dibrugarh City', 'Dibrugarh', 'Assam');

-- Jharkhand
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('834001', 'Ranchi City', 'Ranchi', 'Jharkhand'),
('826001', 'Dhanbad City', 'Dhanbad', 'Jharkhand'),
('831001', 'Jamshedpur City', 'Jamshedpur', 'Jharkhand');

-- Chhattisgarh
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('492001', 'Raipur City', 'Raipur', 'Chhattisgarh'),
('495001', 'Bhilai City', 'Bhilai', 'Chhattisgarh'),
('490001', 'Bilaspur City', 'Bilaspur', 'Chhattisgarh');

-- Uttarakhand
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('248001', 'Dehradun City', 'Dehradun', 'Uttarakhand'),
('263001', 'Nainital City', 'Nainital', 'Uttarakhand'),
('249401', 'Haridwar City', 'Haridwar', 'Uttarakhand');

-- Himachal Pradesh
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('171001', 'Shimla City', 'Shimla', 'Himachal Pradesh'),
('175001', 'Manali City', 'Manali', 'Himachal Pradesh'),
('176001', 'Dharamshala City', 'Dharamshala', 'Himachal Pradesh');

-- Jammu and Kashmir
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('180001', 'Srinagar City', 'Srinagar', 'Jammu and Kashmir'),
('182101', 'Jammu City', 'Jammu', 'Jammu and Kashmir');

-- Goa
INSERT IGNORE INTO Locations (PinCode, AreaName, City, State) VALUES
('403001', 'Panaji City', 'Panaji', 'Goa'),
('403601', 'Margao City', 'Margao', 'Goa');

-- Note: This is a sample dataset with major cities. For production, you would need to import
-- a complete Indian pincode database which contains all 6-digit pin codes in India.
-- You can find such databases from India Post or other official sources.

