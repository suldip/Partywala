DROP DATABASE IF EXISTS PartyClapDB;
CREATE DATABASE PartyClapDB;
USE PartyClapDB;

-- Tables
CREATE TABLE Locations (
    PinCode VARCHAR(10) PRIMARY KEY,
    AreaName VARCHAR(100),
    City VARCHAR(50),
    State VARCHAR(50)
);

CREATE TABLE Customers (
    Id VARCHAR(50) PRIMARY KEY,
    Name VARCHAR(100),
    Email VARCHAR(100) UNIQUE,
    Phone VARCHAR(20),
    PasswordHash VARCHAR(255)
);

CREATE TABLE Admins (
    Id VARCHAR(50) PRIMARY KEY,
    Email VARCHAR(100) UNIQUE,
    PasswordHash VARCHAR(255)
);

CREATE TABLE Vendors (
    Id VARCHAR(50) PRIMARY KEY,
    Name VARCHAR(100),
    Email VARCHAR(100) UNIQUE,
    Phone VARCHAR(20),
    Address TEXT,
    PinCode VARCHAR(10),
    IsRegistered BOOLEAN,
    TrustScore INT,
    WalletBalance DECIMAL(18,2),
    AccountHolderName VARCHAR(100),
    AccountNumber VARCHAR(50),
    IfscCode VARCHAR(20),
    UpiId VARCHAR(100)
    -- PinCode foreign key removed to allow any valid Indian pin code
);

CREATE TABLE Services (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50),
    ServiceType VARCHAR(100),
    Description TEXT,
    Cost DECIMAL(10,2),
    Unit VARCHAR(20),
    MediaUrl VARCHAR(255),
    Attributes TEXT,
    WeekendCost DECIMAL(10,2),
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id)
);

CREATE TABLE VendorPortfolio (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50),
    MediaType VARCHAR(20), -- 'Image' or 'Audio'
    MediaUrl VARCHAR(255),
    Description TEXT,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id)
);

CREATE TABLE Bookings (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50),
    VendorId VARCHAR(50),
    ServiceId VARCHAR(50),
    BookingDate DATETIME,
    EventDate DATETIME,
    VendorCost DECIMAL(18,2),
    CustomerTotalCost DECIMAL(18,2),
    AdvancePaid DECIMAL(18,2),
    BalanceAmount DECIMAL(18,2),
    Status VARCHAR(20),
    BalancePaidOnApp BOOLEAN,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id),
    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);

CREATE TABLE Carts (
    CookieId VARCHAR(50) PRIMARY KEY,
    CreatedDate DATETIME
);

CREATE TABLE CartItems (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CookieId VARCHAR(50),
    ServiceId VARCHAR(50),
    VendorId VARCHAR(50),
    EventDate DATETIME,
    FOREIGN KEY (CookieId) REFERENCES Carts(CookieId),
    FOREIGN KEY (ServiceId) REFERENCES Services(Id),
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id)
);

CREATE TABLE VendorServiceLocations (
    VendorId VARCHAR(50),
    PinCode VARCHAR(10),
    PRIMARY KEY (VendorId, PinCode),
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id)
    -- PinCode foreign key removed to allow any valid Indian pin code
);

CREATE TABLE ServiceRequests (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50),
    VendorId VARCHAR(50),
    ServiceId VARCHAR(50),
    EventDate DATETIME,
    EventType VARCHAR(50),
    GuestCount INT,
    AdditionalDetails TEXT,
    Status VARCHAR(20) DEFAULT 'Pending',
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ResponseDate DATETIME,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id),
    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);

-- Seed Locations
INSERT INTO Locations (PinCode, AreaName, City, State) VALUES 
('110001', 'Connaught Place', 'New Delhi', 'Delhi'),
('110020', 'Hauz Khas', 'New Delhi', 'Delhi'),
('400001', 'Mumbai CST', 'Mumbai', 'Maharashtra'),
('400050', 'Bandra West', 'Mumbai', 'Maharashtra'),
('560001', 'MG Road', 'Bangalore', 'Karnataka'),
('560038', 'Indiranagar', 'Bangalore', 'Karnataka');

-- Seed Admin
INSERT INTO Admins (Id, Email, PasswordHash) VALUES ('admin1', 'admin@partyclap.com', 'admin123');

-- Stored Procedures

DELIMITER //

CREATE PROCEDURE sp_GetLocations()
BEGIN
    SELECT * FROM Locations;
END //

CREATE PROCEDURE sp_RegisterCustomer(
    IN p_Id VARCHAR(50),
    IN p_Name VARCHAR(100),
    IN p_Email VARCHAR(100),
    IN p_Phone VARCHAR(20),
    IN p_PasswordHash VARCHAR(255)
)
BEGIN
    INSERT INTO Customers (Id, Name, Email, Phone, PasswordHash)
    VALUES (p_Id, p_Name, p_Email, p_Phone, p_PasswordHash);
END //

CREATE PROCEDURE sp_RegisterAdmin(
    IN p_Id VARCHAR(50),
    IN p_Email VARCHAR(100),
    IN p_PasswordHash VARCHAR(255)
)
BEGIN
    INSERT INTO Admins (Id, Email, PasswordHash)
    VALUES (p_Id, p_Email, p_PasswordHash);
END //

CREATE PROCEDURE sp_GetCustomerByEmail(IN p_Email VARCHAR(100))
BEGIN
    SELECT * FROM Customers WHERE Email = p_Email;
END //

CREATE PROCEDURE sp_GetAdminByEmail(IN p_Email VARCHAR(100))
BEGIN
    SELECT * FROM Admins WHERE Email = p_Email;
END //

CREATE PROCEDURE sp_RegisterVendor(
    IN p_Id VARCHAR(50),
    IN p_Name VARCHAR(100),
    IN p_Email VARCHAR(100),
    IN p_Phone VARCHAR(20),
    IN p_Address TEXT,
    IN p_PinCode VARCHAR(10),
    IN p_IsRegistered BOOLEAN,
    IN p_TrustScore INT,
    IN p_WalletBalance DECIMAL(18,2),
    IN p_AccountHolderName VARCHAR(100),
    IN p_AccountNumber VARCHAR(50),
    IN p_IfscCode VARCHAR(20),
    IN p_UpiId VARCHAR(100)
)
BEGIN
    INSERT INTO Vendors (Id, Name, Email, Phone, Address, PinCode, IsRegistered, TrustScore, WalletBalance, AccountHolderName, AccountNumber, IfscCode, UpiId)
    VALUES (p_Id, p_Name, p_Email, p_Phone, p_Address, p_PinCode, p_IsRegistered, p_TrustScore, p_WalletBalance, p_AccountHolderName, p_AccountNumber, p_IfscCode, p_UpiId);
END //

CREATE PROCEDURE sp_AddVendorServiceLocation(
    IN p_VendorId VARCHAR(50),
    IN p_PinCode VARCHAR(10)
)
BEGIN
    INSERT INTO VendorServiceLocations (VendorId, PinCode) VALUES (p_VendorId, p_PinCode);
END //

CREATE PROCEDURE sp_GetVendorServiceLocations(IN p_VendorId VARCHAR(50))
BEGIN
    SELECT l.* FROM Locations l
    JOIN VendorServiceLocations vsl ON l.PinCode = vsl.PinCode
    WHERE vsl.VendorId = p_VendorId;
END //

CREATE PROCEDURE sp_GetVendor(IN p_Id VARCHAR(50))
BEGIN
    SELECT * FROM Vendors WHERE Id = p_Id;
END //

CREATE PROCEDURE sp_GetVendorByEmail(IN p_Email VARCHAR(100))
BEGIN
    SELECT * FROM Vendors WHERE Email = p_Email;
END //

CREATE PROCEDURE sp_AddService(
    IN p_Id VARCHAR(50),
    IN p_VendorId VARCHAR(50),
    IN p_ServiceType VARCHAR(100),
    IN p_Description TEXT,
    IN p_Cost DECIMAL(10,2),
    IN p_Unit VARCHAR(20),
    IN p_MediaUrl VARCHAR(255),
    IN p_Attributes TEXT,
    IN p_WeekendCost DECIMAL(10,2)
)
BEGIN
    INSERT INTO Services (Id, VendorId, ServiceType, Description, Cost, Unit, MediaUrl, Attributes, WeekendCost)
    VALUES (p_Id, p_VendorId, p_ServiceType, p_Description, p_Cost, p_Unit, p_MediaUrl, p_Attributes, p_WeekendCost);
END //

CREATE PROCEDURE sp_GetVendorServices(IN p_VendorId VARCHAR(50))
BEGIN
    SELECT * FROM Services WHERE VendorId = p_VendorId;
END //

CREATE PROCEDURE sp_AddPortfolioItem(
    IN p_Id VARCHAR(50),
    IN p_VendorId VARCHAR(50),
    IN p_MediaType VARCHAR(20),
    IN p_MediaUrl VARCHAR(255),
    IN p_Description TEXT
)
BEGIN
    INSERT INTO VendorPortfolio (Id, VendorId, MediaType, MediaUrl, Description)
    VALUES (p_Id, p_VendorId, p_MediaType, p_MediaUrl, p_Description);
END //

CREATE PROCEDURE sp_GetVendorPortfolio(IN p_VendorId VARCHAR(50))
BEGIN
    SELECT * FROM VendorPortfolio WHERE VendorId = p_VendorId;
END //

CREATE PROCEDURE sp_UpdateVendor(
    IN p_Id VARCHAR(50),
    IN p_TrustScore INT,
    IN p_WalletBalance DECIMAL(18,2)
)
BEGIN
    UPDATE Vendors 
    SET TrustScore = p_TrustScore, WalletBalance = p_WalletBalance
    WHERE Id = p_Id;
END //

CREATE PROCEDURE sp_AddToCart(
    IN p_CookieId VARCHAR(50),
    IN p_ServiceId VARCHAR(50),
    IN p_VendorId VARCHAR(50),
    IN p_EventDate DATETIME
)
BEGIN
    -- Ensure Cart Exists
    IF NOT EXISTS (SELECT 1 FROM Carts WHERE CookieId = p_CookieId) THEN
        INSERT INTO Carts (CookieId, CreatedDate) VALUES (p_CookieId, NOW());
    END IF;
    
    -- Add Item
    INSERT INTO CartItems (CookieId, ServiceId, VendorId, EventDate) 
    VALUES (p_CookieId, p_ServiceId, p_VendorId, p_EventDate);
END //

CREATE PROCEDURE sp_GetCartItems(IN p_CookieId VARCHAR(50))
BEGIN
    SELECT c.*, s.ServiceType, s.Cost, s.WeekendCost, v.Name as VendorName 
    FROM CartItems c
    JOIN Services s ON c.ServiceId = s.Id
    JOIN Vendors v ON c.VendorId = v.Id
    WHERE c.CookieId = p_CookieId;
END //

CREATE PROCEDURE sp_RemoveFromCart(IN p_CartItemId INT)
BEGIN
    DELETE FROM CartItems WHERE Id = p_CartItemId;
END //

CREATE PROCEDURE sp_ClearCart(IN p_CookieId VARCHAR(50))
BEGIN
    DELETE FROM CartItems WHERE CookieId = p_CookieId;
END //

CREATE PROCEDURE sp_AddBooking(
    IN p_Id VARCHAR(50),
    IN p_CustomerId VARCHAR(50),
    IN p_VendorId VARCHAR(50),
    IN p_ServiceId VARCHAR(50),
    IN p_BookingDate DATETIME,
    IN p_EventDate DATETIME,
    IN p_VendorCost DECIMAL(18,2),
    IN p_CustomerTotalCost DECIMAL(18,2),
    IN p_AdvancePaid DECIMAL(18,2),
    IN p_BalanceAmount DECIMAL(18,2),
    IN p_Status VARCHAR(20),
    IN p_BalancePaidOnApp BOOLEAN
)
BEGIN
    INSERT INTO Bookings (Id, CustomerId, VendorId, ServiceId, BookingDate, EventDate, VendorCost, CustomerTotalCost, AdvancePaid, BalanceAmount, Status, BalancePaidOnApp)
    VALUES (p_Id, p_CustomerId, p_VendorId, p_ServiceId, p_BookingDate, p_EventDate, p_VendorCost, p_CustomerTotalCost, p_AdvancePaid, p_BalanceAmount, p_Status, p_BalancePaidOnApp);
END //

CREATE PROCEDURE sp_GetVendorBookings(IN p_VendorId VARCHAR(50))
BEGIN
    SELECT * FROM Bookings WHERE VendorId = p_VendorId;
END //

CREATE PROCEDURE sp_UpdateBookingStatus(
    IN p_Id VARCHAR(50),
    IN p_Status VARCHAR(20),
    IN p_BalancePaidOnApp BOOLEAN,
    IN p_VendorCost DECIMAL(10,2),
    IN p_CustomerTotalCost DECIMAL(10,2)
)
BEGIN
    UPDATE Bookings 
    SET Status = p_Status, 
        BalancePaidOnApp = p_BalancePaidOnApp,
        VendorCost = IF(p_VendorCost IS NULL, VendorCost, p_VendorCost),
        CustomerTotalCost = IF(p_CustomerTotalCost IS NULL, CustomerTotalCost, p_CustomerTotalCost),
        BalanceAmount = IF(p_CustomerTotalCost IS NULL, BalanceAmount, p_CustomerTotalCost - AdvancePaid)
    WHERE Id = p_Id;
END //

CREATE PROCEDURE sp_GetCustomerBookings(IN p_CustomerId VARCHAR(50))
BEGIN
    SELECT b.*, v.Name as VendorName, s.ServiceType 
    FROM Bookings b
    JOIN Vendors v ON b.VendorId = v.Id
    JOIN Services s ON b.ServiceId = s.Id
    WHERE b.CustomerId = p_CustomerId
    ORDER BY b.BookingDate DESC;
END //

CREATE PROCEDURE sp_SearchServices(
    IN p_SearchTerm VARCHAR(100),
    IN p_PinCode VARCHAR(10),
    IN p_MinPrice DECIMAL(10,2),
    IN p_MaxPrice DECIMAL(10,2),
    IN p_MinRating INT,
    IN p_EventDate DATETIME
)
BEGIN
    SELECT s.*, v.Name as VendorName 
    FROM Services s
    JOIN Vendors v ON s.VendorId = v.Id
    LEFT JOIN VendorServiceLocations vsl ON v.Id = vsl.VendorId
    WHERE (p_SearchTerm IS NULL OR s.ServiceType LIKE CONCAT('%', p_SearchTerm, '%') OR s.Description LIKE CONCAT('%', p_SearchTerm, '%'))
    AND (p_PinCode IS NULL OR v.PinCode = p_PinCode OR vsl.PinCode = p_PinCode)
    AND (p_MinPrice IS NULL OR s.Cost >= p_MinPrice)
    AND (p_MaxPrice IS NULL OR s.Cost <= p_MaxPrice)
    -- MinRating logic would go here if we had ratings
    ;
END //

DELIMITER ;
