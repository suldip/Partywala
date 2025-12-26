-- Migration script to remove PinCode foreign key constraints
-- Run this if your database already exists and you need to remove the foreign key constraints

USE PartyClapDB;

-- Remove foreign key constraint from Vendors table
SET @dbname = DATABASE();
SET @tablename = 'Vendors';
SET @constraintname = 'vendors_ibfk_1';

SET @preparedStatement = (SELECT IF(
    (
        SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE
            (TABLE_SCHEMA = @dbname)
            AND (TABLE_NAME = @tablename)
            AND (CONSTRAINT_NAME = @constraintname)
            AND (CONSTRAINT_TYPE = 'FOREIGN KEY')
    ) > 0,
    CONCAT('ALTER TABLE ', @tablename, ' DROP FOREIGN KEY ', @constraintname),
    'SELECT 1'
));
PREPARE alterIfExists FROM @preparedStatement;
EXECUTE alterIfExists;
DEALLOCATE PREPARE alterIfExists;

-- Remove foreign key constraint from VendorServiceLocations table
SET @tablename2 = 'VendorServiceLocations';
SET @constraintname2 = 'vendorservicelocations_ibfk_2';

SET @preparedStatement = (SELECT IF(
    (
        SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE
            (TABLE_SCHEMA = @dbname)
            AND (TABLE_NAME = @tablename2)
            AND (CONSTRAINT_NAME = @constraintname2)
            AND (CONSTRAINT_TYPE = 'FOREIGN KEY')
    ) > 0,
    CONCAT('ALTER TABLE ', @tablename2, ' DROP FOREIGN KEY ', @constraintname2),
    'SELECT 1'
));
PREPARE alterIfExists FROM @preparedStatement;
EXECUTE alterIfExists;
DEALLOCATE PREPARE alterIfExists;

