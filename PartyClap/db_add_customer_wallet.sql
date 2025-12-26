-- Add WalletBalance column to Customers table (Safe Migration)
USE PartyClapDB;

-- Check if WalletBalance column exists, if not add it
SET @dbname = 'PartyClapDB';
SET @tablename = 'Customers';
SET @columnname = 'WalletBalance';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      (TABLE_SCHEMA = @dbname)
      AND (TABLE_NAME = @tablename)
      AND (COLUMN_NAME = @columnname)
  ) > 0,
  'SELECT ''Column already exists'' AS Message;',
  'ALTER TABLE Customers ADD COLUMN WalletBalance DECIMAL(18,2) DEFAULT 0.00;'
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;

-- Create WalletTransactions table to track all wallet activities
-- Drop table if exists to ensure schema is correct (fixes 'Unknown column' error)
DROP TABLE IF EXISTS WalletTransactions;
CREATE TABLE WalletTransactions (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50) NOT NULL,
    TransactionType VARCHAR(20) NOT NULL, -- 'Credit', 'Debit', 'Refund'
    Amount DECIMAL(18,2) NOT NULL,
    Description TEXT,
    TransactionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    BookingId VARCHAR(50) NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id)
);

-- Update existing customers to have 0 wallet balance if NULL
UPDATE Customers SET WalletBalance = 0.00 WHERE WalletBalance IS NULL;

SELECT 'Customer wallet schema updated successfully!' AS Message;
SELECT CONCAT('WalletTransactions table: ', IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
     WHERE TABLE_SCHEMA = 'PartyClapDB' AND TABLE_NAME = 'WalletTransactions') > 0,
    'EXISTS ✓',
    'NOT FOUND ✗'
)) AS Status;
