USE PartyClapDB;

CREATE TABLE IF NOT EXISTS Reviews (
    Id VARCHAR(50) PRIMARY KEY,
    BookingId VARCHAR(50) NOT NULL,
    CustomerId VARCHAR(50) NOT NULL,
    VendorId VARCHAR(50) NOT NULL,
    ServiceId VARCHAR(50) NOT NULL,
    Rating INT NOT NULL,
    Comment TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id),
    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);

DROP PROCEDURE IF EXISTS sp_AddReview;
DROP PROCEDURE IF EXISTS sp_GetServiceReviews;
DROP PROCEDURE IF EXISTS sp_GetVendorReviews;

DELIMITER //

CREATE PROCEDURE sp_AddReview(
    IN p_Id VARCHAR(50),
    IN p_BookingId VARCHAR(50),
    IN p_CustomerId VARCHAR(50),
    IN p_VendorId VARCHAR(50),
    IN p_ServiceId VARCHAR(50),
    IN p_Rating INT,
    IN p_Comment TEXT
)
BEGIN
    INSERT INTO Reviews (Id, BookingId, CustomerId, VendorId, ServiceId, Rating, Comment, CreatedAt)
    VALUES (p_Id, p_BookingId, p_CustomerId, p_VendorId, p_ServiceId, p_Rating, p_Comment, NOW());
END //

CREATE PROCEDURE sp_GetServiceReviews(IN p_ServiceId VARCHAR(50))
BEGIN
    SELECT r.*, c.Name as CustomerName 
    FROM Reviews r
    JOIN Customers c ON r.CustomerId = c.Id
    WHERE r.ServiceId = p_ServiceId
    ORDER BY r.CreatedAt DESC;
END //

CREATE PROCEDURE sp_GetVendorReviews(IN p_VendorId VARCHAR(50))
BEGIN
    SELECT r.*, c.Name as CustomerName, s.ServiceType as ServiceName
    FROM Reviews r
    JOIN Customers c ON r.CustomerId = c.Id
    JOIN Services s ON r.ServiceId = s.Id
    WHERE r.VendorId = p_VendorId
    ORDER BY r.CreatedAt DESC;
END //

DELIMITER ;
