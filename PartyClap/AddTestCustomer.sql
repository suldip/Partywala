-- Add Test Customer for Login Testing
-- This script adds a test customer to the Customers table

INSERT INTO Customers (Id, Name, Email, Phone, PasswordHash)
VALUES 
(UUID(), 'Test Customer', 'customer@test.com', '9999999999', 'password123');

-- Verify insertion
SELECT * FROM Customers WHERE Email = 'customer@test.com';

-- Test Login Credentials:
-- Email: customer@test.com
-- Password: password123
