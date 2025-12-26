USE PartyClapDB;

CREATE TABLE IF NOT EXISTS Disputes (
    Id VARCHAR(50) PRIMARY KEY,
    BookingId VARCHAR(50) NOT NULL,
    RaisedByUserId VARCHAR(50) NOT NULL,
    UserRole VARCHAR(20) NOT NULL,
    Reason VARCHAR(100) NOT NULL,
    Description TEXT,
    Status VARCHAR(30) DEFAULT 'Open',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    Resolution TEXT,
    ResolvedAt DATETIME,
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id)
);

DROP PROCEDURE IF EXISTS sp_RaiseDispute;
DROP PROCEDURE IF EXISTS sp_GetDisputeById;
DROP PROCEDURE IF EXISTS sp_GetDisputesByBooking;

DELIMITER //

CREATE PROCEDURE sp_RaiseDispute(
    IN p_Id VARCHAR(50),
    IN p_BookingId VARCHAR(50),
    IN p_RaisedByUserId VARCHAR(50),
    IN p_UserRole VARCHAR(20),
    IN p_Reason VARCHAR(100),
    IN p_Description TEXT
)
BEGIN
    INSERT INTO Disputes (Id, BookingId, RaisedByUserId, UserRole, Reason, Description, Status, CreatedAt)
    VALUES (p_Id, p_BookingId, p_RaisedByUserId, p_UserRole, p_Reason, p_Description, 'Open', NOW());
END //

CREATE PROCEDURE sp_GetDisputeById(IN p_Id VARCHAR(50))
BEGIN
    SELECT * FROM Disputes WHERE Id = p_Id;
END //

CREATE PROCEDURE sp_GetDisputesByBooking(IN p_BookingId VARCHAR(50))
BEGIN
    SELECT * FROM Disputes WHERE BookingId = p_BookingId;
END //

DELIMITER ;
