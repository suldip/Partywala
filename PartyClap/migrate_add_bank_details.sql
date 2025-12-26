-- Migration script to add bank details columns to Vendors table
-- Run this if your database already exists and you need to add the bank details columns

USE PartyClapDB;

-- Add bank details columns to Vendors table if they don't exist
-- MySQL doesn't support IF NOT EXISTS with ADD COLUMN, so we use a stored procedure approach

DELIMITER //

CREATE PROCEDURE AddColumnIfNotExists(
    IN tableName VARCHAR(64),
    IN columnName VARCHAR(64),
    IN columnDefinition TEXT
)
BEGIN
    DECLARE columnExists INT DEFAULT 0;
    
    SELECT COUNT(*) INTO columnExists
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = tableName
      AND COLUMN_NAME = columnName;
    
    IF columnExists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE ', tableName, ' ADD COLUMN ', columnName, ' ', columnDefinition);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END //

DELIMITER ;

-- Add columns using the procedure
CALL AddColumnIfNotExists('Vendors', 'AccountHolderName', 'VARCHAR(100) NULL');
CALL AddColumnIfNotExists('Vendors', 'AccountNumber', 'VARCHAR(50) NULL');
CALL AddColumnIfNotExists('Vendors', 'IfscCode', 'VARCHAR(20) NULL');
CALL AddColumnIfNotExists('Vendors', 'UpiId', 'VARCHAR(100) NULL');

-- Drop the temporary procedure
DROP PROCEDURE IF EXISTS AddColumnIfNotExists;

-- Update the stored procedure
DROP PROCEDURE IF EXISTS sp_RegisterVendor;

DELIMITER //

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

DELIMITER ;

