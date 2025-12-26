# Customer Wallet Feature - Setup Instructions

## Database Migration

Before running the application, you need to run the database migration to add wallet functionality:

### Option 1: Using MySQL Workbench
1. Open MySQL Workbench
2. Connect to your PartyClapDB database
3. Open the file: `db_add_customer_wallet.sql`
4. Execute the script

### Option 2: Using Command Line
If MySQL is in your PATH, run:
```bash
mysql -u root -p < db_add_customer_wallet.sql
```
Enter your MySQL password when prompted.

### Option 3: Using MySQL Command Line Client
1. Open MySQL Command Line Client
2. Enter your password
3. Run: `USE PartyClapDB;`
4. Run: `source d:/BabuSir/PartyClap/PartyClap/db_add_customer_wallet.sql;`

## What This Migration Does

1. Adds a `WalletBalance` column to the `Customers` table
2. Creates a new `WalletTransactions` table to track all wallet activities
3. Sets default wallet balance to 0.00 for all existing customers

## Features Added

### Customer Dashboard Now Includes:
- **Wallet Card**: Beautiful gradient card showing current wallet balance
- **Add Money**: Quick add money functionality with preset amounts
- **Transaction History**: View recent wallet transactions
- **Quick Stats**: Overview of bookings and service requests
- **Modern UI**: Premium design with smooth animations

### Wallet Functionality:
- Add money to wallet (₹1 to ₹1,00,000 per transaction)
- View transaction history
- Track credits, debits, and refunds
- Secure transaction recording

## Testing the Feature

1. Run the database migration
2. Build and run the application
3. Login as a customer
4. Navigate to Dashboard
5. Click "Add Money" on the wallet card
6. Select an amount or enter a custom value
7. Submit to add money to your wallet

The wallet balance will be updated and displayed on the dashboard.
