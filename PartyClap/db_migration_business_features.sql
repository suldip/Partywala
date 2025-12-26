-- =====================================================
-- Business Features Database Migration
-- PartyClap Platform
-- =====================================================
-- This script adds all tables required for:
-- 1. Vendor Incentive Plans
-- 2. Scoring and Reward System
-- 3. Customer Rewards
-- 4. Cancellation Handling
-- 5. Vendor Insurance
-- 6. Platform Professional Indemnity
-- 7. Interest-Bearing Wallets
-- =====================================================

USE PartyClapDB;

-- =====================================================
-- 1. VENDOR INCENTIVE SYSTEM
-- =====================================================

CREATE TABLE IF NOT EXISTS VendorIncentives (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50) NOT NULL,
    MonthYear VARCHAR(7) NOT NULL, -- Format: YYYY-MM
    TotalBookings INT DEFAULT 0,
    TotalEarnings DECIMAL(18,2) DEFAULT 0,
    Tier VARCHAR(20) DEFAULT 'Bronze', -- Bronze, Silver, Gold, Platinum
    CommissionRate DECIMAL(5,2) DEFAULT 10.00,
    CashbackAmount DECIMAL(18,2) DEFAULT 0,
    BonusAmount DECIMAL(18,2) DEFAULT 0,
    TotalIncentive DECIMAL(18,2) DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Processed, Paid
    ProcessedDate DATETIME,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id) ON DELETE CASCADE,
    UNIQUE KEY unique_vendor_month (VendorId, MonthYear),
    INDEX idx_vendor (VendorId),
    INDEX idx_month (MonthYear),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS IncentiveTransactions (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50) NOT NULL,
    IncentiveId VARCHAR(50),
    TransactionType VARCHAR(20) NOT NULL, -- Cashback, Bonus, TierUpgrade
    Amount DECIMAL(18,2) NOT NULL,
    Description TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id) ON DELETE CASCADE,
    FOREIGN KEY (IncentiveId) REFERENCES VendorIncentives(Id) ON DELETE SET NULL,
    INDEX idx_vendor (VendorId),
    INDEX idx_type (TransactionType)
);

-- =====================================================
-- 2. SCORING AND REWARD SYSTEM
-- =====================================================

CREATE TABLE IF NOT EXISTS VendorScores (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50) NOT NULL UNIQUE,
    TrustScore INT DEFAULT 100,
    TotalBookings INT DEFAULT 0,
    CompletedBookings INT DEFAULT 0,
    CancelledBookings INT DEFAULT 0,
    NoShowCount INT DEFAULT 0,
    AverageRating DECIMAL(3,2) DEFAULT 0,
    TotalRatings INT DEFAULT 0,
    OnTimePercentage DECIMAL(5,2) DEFAULT 100,
    AverageResponseTimeMinutes INT DEFAULT 0,
    ComplaintCount INT DEFAULT 0,
    RewardLevel VARCHAR(20) DEFAULT 'New', -- New, Trusted, Verified, Premium, Elite
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id) ON DELETE CASCADE,
    INDEX idx_score (TrustScore),
    INDEX idx_level (RewardLevel)
);

CREATE TABLE IF NOT EXISTS ScoreHistory (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50) NOT NULL,
    ScoreChange INT NOT NULL, -- Positive or negative
    ScoreBefore INT NOT NULL,
    ScoreAfter INT NOT NULL,
    Reason VARCHAR(100) NOT NULL, -- BookingCompleted, RatingReceived, Cancellation, etc.
    BookingId VARCHAR(50),
    Details TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id) ON DELETE CASCADE,
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id) ON DELETE SET NULL,
    INDEX idx_vendor (VendorId),
    INDEX idx_date (CreatedDate)
);

CREATE TABLE IF NOT EXISTS CustomerRewards (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50) NOT NULL UNIQUE,
    LoyaltyPoints INT DEFAULT 0,
    TotalSpent DECIMAL(18,2) DEFAULT 0,
    CashbackBalance DECIMAL(18,2) DEFAULT 0,
    ReferralCode VARCHAR(20) UNIQUE,
    ReferralCount INT DEFAULT 0,
    ReviewCount INT DEFAULT 0,
    BirthdayMonth INT,
    AnniversaryDate DATE,
    CustomerTier VARCHAR(20) DEFAULT 'Regular', -- Regular, Premium, VIP
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE,
    INDEX idx_tier (CustomerTier),
    INDEX idx_points (LoyaltyPoints)
);

CREATE TABLE IF NOT EXISTS RewardTransactions (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50) NOT NULL,
    Points INT DEFAULT 0,
    CashbackAmount DECIMAL(18,2) DEFAULT 0,
    TransactionType VARCHAR(50) NOT NULL, -- Purchase, Referral, Review, Redemption, Birthday, Anniversary
    Description TEXT,
    BookingId VARCHAR(50),
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE,
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id) ON DELETE SET NULL,
    INDEX idx_customer (CustomerId),
    INDEX idx_type (TransactionType)
);

CREATE TABLE IF NOT EXISTS RewardRedemptions (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50) NOT NULL,
    RewardType VARCHAR(50) NOT NULL, -- Points, Cashback, Referral
    PointsUsed INT DEFAULT 0,
    CashbackUsed DECIMAL(18,2) DEFAULT 0,
    DiscountAmount DECIMAL(18,2) NOT NULL,
    BookingId VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'Applied', -- Applied, Used, Expired, Cancelled
    ExpiryDate DATETIME,
    UsedDate DATETIME,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE,
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id) ON DELETE SET NULL,
    INDEX idx_customer (CustomerId),
    INDEX idx_status (Status)
);

-- =====================================================
-- 3. CANCELLATION HANDLING
-- =====================================================

-- Add cancellation columns to Bookings table if they don't exist
-- MySQL doesn't support IF NOT EXISTS for ALTER TABLE, so we use a stored procedure

DELIMITER //

CREATE PROCEDURE IF NOT EXISTS sp_AddCancellationColumns()
BEGIN
    DECLARE CONTINUE HANDLER FOR SQLEXCEPTION BEGIN END;
    
    -- Check and add CancellationDate
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = DATABASE() 
        AND TABLE_NAME = 'Bookings' 
        AND COLUMN_NAME = 'CancellationDate'
    ) THEN
        ALTER TABLE Bookings ADD COLUMN CancellationDate DATETIME;
    END IF;
    
    -- Check and add CancelledBy
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = DATABASE() 
        AND TABLE_NAME = 'Bookings' 
        AND COLUMN_NAME = 'CancelledBy'
    ) THEN
        ALTER TABLE Bookings ADD COLUMN CancelledBy VARCHAR(20);
    END IF;
    
    -- Check and add CancelledById
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = DATABASE() 
        AND TABLE_NAME = 'Bookings' 
        AND COLUMN_NAME = 'CancelledById'
    ) THEN
        ALTER TABLE Bookings ADD COLUMN CancelledById VARCHAR(50);
    END IF;
    
    -- Check and add CancellationReason
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = DATABASE() 
        AND TABLE_NAME = 'Bookings' 
        AND COLUMN_NAME = 'CancellationReason'
    ) THEN
        ALTER TABLE Bookings ADD COLUMN CancellationReason VARCHAR(100);
    END IF;
    
    -- Check and add RefundAmount
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = DATABASE() 
        AND TABLE_NAME = 'Bookings' 
        AND COLUMN_NAME = 'RefundAmount'
    ) THEN
        ALTER TABLE Bookings ADD COLUMN RefundAmount DECIMAL(18,2) DEFAULT 0;
    END IF;
    
    -- Check and add CompensationAmount
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = DATABASE() 
        AND TABLE_NAME = 'Bookings' 
        AND COLUMN_NAME = 'CompensationAmount'
    ) THEN
        ALTER TABLE Bookings ADD COLUMN CompensationAmount DECIMAL(18,2) DEFAULT 0;
    END IF;
    
    -- Check and add RefundStatus
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = DATABASE() 
        AND TABLE_NAME = 'Bookings' 
        AND COLUMN_NAME = 'RefundStatus'
    ) THEN
        ALTER TABLE Bookings ADD COLUMN RefundStatus VARCHAR(20) DEFAULT 'Pending';
    END IF;
    
    -- Check and add RefundProcessedDate
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = DATABASE() 
        AND TABLE_NAME = 'Bookings' 
        AND COLUMN_NAME = 'RefundProcessedDate'
    ) THEN
        ALTER TABLE Bookings ADD COLUMN RefundProcessedDate DATETIME;
    END IF;
END //

DELIMITER ;

-- Execute the procedure to add columns
CALL sp_AddCancellationColumns();

-- Drop the temporary procedure
DROP PROCEDURE IF EXISTS sp_AddCancellationColumns;

CREATE TABLE IF NOT EXISTS Cancellations (
    Id VARCHAR(50) PRIMARY KEY,
    BookingId VARCHAR(50) NOT NULL,
    CancelledBy VARCHAR(20) NOT NULL, -- Customer, Vendor, Admin
    CancelledById VARCHAR(50) NOT NULL, -- CustomerId or VendorId
    CancellationReason VARCHAR(100),
    CancellationDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    DaysBeforeEvent INT,
    RefundAmount DECIMAL(18,2) DEFAULT 0,
    CompensationAmount DECIMAL(18,2) DEFAULT 0,
    RefundStatus VARCHAR(20) DEFAULT 'Pending', -- Pending, Processed, Failed, Partial
    RefundProcessedDate DATETIME,
    RefundMethod VARCHAR(20), -- Original, Wallet, Bank
    RefundTransactionId VARCHAR(100),
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id) ON DELETE CASCADE,
    INDEX idx_booking (BookingId),
    INDEX idx_status (RefundStatus),
    INDEX idx_date (CancellationDate)
);

-- =====================================================
-- 4. VENDOR INSURANCE
-- =====================================================

CREATE TABLE IF NOT EXISTS VendorInsurance (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50) NOT NULL,
    PolicyNumber VARCHAR(50) UNIQUE,
    InsuranceType VARCHAR(50) NOT NULL, -- PublicLiability, Equipment, ProfessionalIndemnity, PersonalAccident
    CoverageAmount DECIMAL(18,2) NOT NULL,
    PremiumAmount DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Status VARCHAR(20) DEFAULT 'Active', -- Active, Expired, Cancelled, Suspended
    AutoRenew BOOLEAN DEFAULT TRUE,
    LastPaymentDate DATETIME,
    NextPaymentDate DATETIME,
    PaymentMethod VARCHAR(20), -- Wallet, Bank, UPI
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id) ON DELETE CASCADE,
    INDEX idx_vendor (VendorId),
    INDEX idx_status (Status),
    INDEX idx_end_date (EndDate)
);

CREATE TABLE IF NOT EXISTS InsuranceClaims (
    Id VARCHAR(50) PRIMARY KEY,
    InsuranceId VARCHAR(50) NOT NULL,
    VendorId VARCHAR(50) NOT NULL,
    BookingId VARCHAR(50),
    ClaimType VARCHAR(50) NOT NULL, -- PropertyDamage, PersonalInjury, EquipmentLoss, etc.
    ClaimAmount DECIMAL(18,2) NOT NULL,
    IncidentDate DATETIME NOT NULL,
    Description TEXT NOT NULL,
    SupportingDocuments TEXT, -- JSON array of file URLs
    Status VARCHAR(20) DEFAULT 'Submitted', -- Submitted, UnderReview, Approved, Rejected, Paid
    ApprovedAmount DECIMAL(18,2),
    ProcessedDate DATETIME,
    RejectionReason TEXT,
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (InsuranceId) REFERENCES VendorInsurance(Id) ON DELETE CASCADE,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id) ON DELETE CASCADE,
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id) ON DELETE SET NULL,
    INDEX idx_insurance (InsuranceId),
    INDEX idx_vendor (VendorId),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS InsurancePayments (
    Id VARCHAR(50) PRIMARY KEY,
    InsuranceId VARCHAR(50) NOT NULL,
    VendorId VARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    PaymentMethod VARCHAR(20) NOT NULL, -- Wallet, Bank, UPI
    TransactionId VARCHAR(100),
    Status VARCHAR(20) DEFAULT 'Completed', -- Pending, Completed, Failed
    FailureReason TEXT,
    FOREIGN KEY (InsuranceId) REFERENCES VendorInsurance(Id) ON DELETE CASCADE,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id) ON DELETE CASCADE,
    INDEX idx_insurance (InsuranceId),
    INDEX idx_vendor (VendorId),
    INDEX idx_date (PaymentDate)
);

-- =====================================================
-- 5. PLATFORM PROFESSIONAL INDEMNITY
-- =====================================================

CREATE TABLE IF NOT EXISTS PlatformInsurance (
    Id VARCHAR(50) PRIMARY KEY,
    PolicyNumber VARCHAR(50) UNIQUE NOT NULL,
    InsuranceProvider VARCHAR(100) NOT NULL,
    CoverageAmount DECIMAL(18,2) NOT NULL,
    PremiumAmount DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    Status VARCHAR(20) DEFAULT 'Active', -- Active, Expired, Cancelled
    ContactPerson VARCHAR(100),
    ContactEmail VARCHAR(100),
    ContactPhone VARCHAR(20),
    Documents TEXT, -- JSON array of policy documents
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_status (Status),
    INDEX idx_end_date (EndDate)
);

CREATE TABLE IF NOT EXISTS PlatformClaims (
    Id VARCHAR(50) PRIMARY KEY,
    InsuranceId VARCHAR(50) NOT NULL,
    ClaimType VARCHAR(50) NOT NULL, -- Liability, DataBreach, ServiceFailure, PropertyDamage
    IncidentDate DATETIME NOT NULL,
    Description TEXT NOT NULL,
    ClaimAmount DECIMAL(18,2) NOT NULL,
    AffectedParties INT DEFAULT 0, -- Number of customers/vendors affected
    SupportingDocuments TEXT, -- JSON array of file URLs
    Status VARCHAR(20) DEFAULT 'Submitted', -- Submitted, UnderReview, Approved, Rejected, Paid
    ApprovedAmount DECIMAL(18,2),
    ProcessedDate DATETIME,
    RejectionReason TEXT,
    Notes TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (InsuranceId) REFERENCES PlatformInsurance(Id) ON DELETE CASCADE,
    INDEX idx_insurance (InsuranceId),
    INDEX idx_status (Status),
    INDEX idx_date (IncidentDate)
);

-- =====================================================
-- 6. INTEREST-BEARING WALLETS
-- =====================================================

CREATE TABLE IF NOT EXISTS Wallets (
    Id VARCHAR(50) PRIMARY KEY,
    OwnerId VARCHAR(50) NOT NULL, -- VendorId or CustomerId
    OwnerType VARCHAR(20) NOT NULL, -- Vendor, Customer
    Balance DECIMAL(18,2) DEFAULT 0,
    LockedBalance DECIMAL(18,2) DEFAULT 0, -- For pending transactions
    -- AvailableBalance: Calculated column (requires MySQL 5.7.5+)
    -- If you get an error, remove this line and calculate in application code
    AvailableBalance DECIMAL(18,2) GENERATED ALWAYS AS (Balance - LockedBalance) STORED,
    InterestRate DECIMAL(5,2) DEFAULT 4.00,
    LastInterestDate DATE,
    TotalInterestEarned DECIMAL(18,2) DEFAULT 0,
    MinimumBalanceForInterest DECIMAL(18,2) DEFAULT 1000,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY unique_owner (OwnerId, OwnerType),
    INDEX idx_owner (OwnerId, OwnerType),
    INDEX idx_balance (Balance)
);

CREATE TABLE IF NOT EXISTS WalletTransactions (
    Id VARCHAR(50) PRIMARY KEY,
    WalletId VARCHAR(50) NOT NULL,
    TransactionType VARCHAR(50) NOT NULL, -- Credit, Debit, Interest, Refund, Withdrawal, Insurance, Incentive
    Amount DECIMAL(18,2) NOT NULL,
    BalanceBefore DECIMAL(18,2) NOT NULL,
    BalanceAfter DECIMAL(18,2) NOT NULL,
    Description TEXT,
    ReferenceId VARCHAR(50), -- BookingId, IncentiveId, CancellationId, etc.
    ReferenceType VARCHAR(50), -- Booking, Incentive, Refund, Insurance, etc.
    Status VARCHAR(20) DEFAULT 'Completed', -- Pending, Completed, Failed, Reversed
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (WalletId) REFERENCES Wallets(Id) ON DELETE CASCADE,
    INDEX idx_wallet (WalletId),
    INDEX idx_type (TransactionType),
    INDEX idx_date (CreatedDate),
    INDEX idx_reference (ReferenceId, ReferenceType)
);

CREATE TABLE IF NOT EXISTS InterestCalculations (
    Id VARCHAR(50) PRIMARY KEY,
    WalletId VARCHAR(50) NOT NULL,
    MonthYear VARCHAR(7) NOT NULL, -- Format: YYYY-MM
    AverageDailyBalance DECIMAL(18,2) NOT NULL,
    InterestRate DECIMAL(5,2) NOT NULL,
    InterestAmount DECIMAL(18,2) NOT NULL,
    DaysInMonth INT NOT NULL,
    CalculatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    CreditedDate DATETIME,
    Status VARCHAR(20) DEFAULT 'Calculated', -- Calculated, Credited, Failed
    Notes TEXT,
    FOREIGN KEY (WalletId) REFERENCES Wallets(Id) ON DELETE CASCADE,
    UNIQUE KEY unique_wallet_month (WalletId, MonthYear),
    INDEX idx_wallet (WalletId),
    INDEX idx_month (MonthYear),
    INDEX idx_status (Status)
);

CREATE TABLE IF NOT EXISTS Withdrawals (
    Id VARCHAR(50) PRIMARY KEY,
    WalletId VARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    WithdrawalMethod VARCHAR(20) NOT NULL, -- UPI, Bank, Wallet
    UpiId VARCHAR(100),
    BankAccountNumber VARCHAR(50),
    IfscCode VARCHAR(20),
    AccountHolderName VARCHAR(100),
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Processing, Completed, Failed, Cancelled
    ProcessedDate DATETIME,
    TransactionId VARCHAR(100),
    FailureReason TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (WalletId) REFERENCES Wallets(Id) ON DELETE CASCADE,
    INDEX idx_wallet (WalletId),
    INDEX idx_status (Status),
    INDEX idx_date (CreatedDate)
);

-- =====================================================
-- STORED PROCEDURES
-- =====================================================

DELIMITER //

-- Calculate vendor tier based on monthly bookings
CREATE PROCEDURE IF NOT EXISTS sp_CalculateVendorTier(
    IN p_VendorId VARCHAR(50),
    IN p_MonthYear VARCHAR(7)
)
BEGIN
    DECLARE v_Bookings INT;
    DECLARE v_Tier VARCHAR(20);
    DECLARE v_CommissionRate DECIMAL(5,2);
    
    SELECT COUNT(*) INTO v_Bookings
    FROM Bookings
    WHERE VendorId = p_VendorId
    AND DATE_FORMAT(BookingDate, '%Y-%m') = p_MonthYear
    AND Status IN ('Confirmed', 'Completed');
    
    IF v_Bookings >= 51 THEN
        SET v_Tier = 'Platinum';
        SET v_CommissionRate = 7.00;
    ELSEIF v_Bookings >= 26 THEN
        SET v_Tier = 'Gold';
        SET v_CommissionRate = 8.00;
    ELSEIF v_Bookings >= 11 THEN
        SET v_Tier = 'Silver';
        SET v_CommissionRate = 9.00;
    ELSE
        SET v_Tier = 'Bronze';
        SET v_CommissionRate = 10.00;
    END IF;
    
    SELECT v_Tier as Tier, v_CommissionRate as CommissionRate, v_Bookings as Bookings;
END //

-- Calculate interest for a wallet
CREATE PROCEDURE IF NOT EXISTS sp_CalculateWalletInterest(
    IN p_WalletId VARCHAR(50),
    IN p_MonthYear VARCHAR(7)
)
BEGIN
    DECLARE v_AvgBalance DECIMAL(18,2);
    DECLARE v_InterestRate DECIMAL(5,2);
    DECLARE v_DaysInMonth INT;
    DECLARE v_InterestAmount DECIMAL(18,2);
    DECLARE v_MinBalance DECIMAL(18,2);
    
    -- Get wallet details
    SELECT InterestRate, MinimumBalanceForInterest INTO v_InterestRate, v_MinBalance
    FROM Wallets WHERE Id = p_WalletId;
    
    -- Calculate average daily balance for the month
    SELECT 
        COALESCE(AVG(BalanceAfter), 0) INTO v_AvgBalance
    FROM WalletTransactions
    WHERE WalletId = p_WalletId
    AND DATE_FORMAT(CreatedDate, '%Y-%m') = p_MonthYear;
    
    -- Get days in month
    SET v_DaysInMonth = DAY(LAST_DAY(STR_TO_DATE(CONCAT(p_MonthYear, '-01'), '%Y-%m-%d')));
    
    -- Calculate interest only if balance >= minimum
    IF v_AvgBalance >= v_MinBalance THEN
        SET v_InterestAmount = (v_AvgBalance * v_InterestRate / 100) * (v_DaysInMonth / 365);
    ELSE
        SET v_InterestAmount = 0;
    END IF;
    
    SELECT v_InterestAmount as InterestAmount, v_AvgBalance as AverageBalance;
END //

-- Update vendor trust score
CREATE PROCEDURE IF NOT EXISTS sp_UpdateVendorScore(
    IN p_VendorId VARCHAR(50),
    IN p_ScoreChange INT,
    IN p_Reason VARCHAR(100),
    IN p_BookingId VARCHAR(50)
)
BEGIN
    DECLARE v_CurrentScore INT;
    DECLARE v_NewScore INT;
    DECLARE v_ScoreId VARCHAR(50);
    
    -- Get current score
    SELECT TrustScore INTO v_CurrentScore
    FROM VendorScores
    WHERE VendorId = p_VendorId;
    
    IF v_CurrentScore IS NULL THEN
        -- Create score record if doesn't exist
        SET v_ScoreId = CONCAT(p_VendorId, '_', UNIX_TIMESTAMP(), '_', FLOOR(RAND() * 1000));
        INSERT INTO VendorScores (Id, VendorId, TrustScore)
        VALUES (v_ScoreId, p_VendorId, 100);
        SET v_CurrentScore = 100;
    END IF;
    
    -- Calculate new score (clamp between 0 and 1000)
    SET v_NewScore = GREATEST(0, LEAST(1000, v_CurrentScore + p_ScoreChange));
    
    -- Update score
    UPDATE VendorScores
    SET TrustScore = v_NewScore,
        LastUpdated = NOW()
    WHERE VendorId = p_VendorId;
    
    -- Add to history
    INSERT INTO ScoreHistory (Id, VendorId, ScoreChange, ScoreBefore, ScoreAfter, Reason, BookingId)
    VALUES (CONCAT(p_VendorId, '_', UNIX_TIMESTAMP(), '_', FLOOR(RAND() * 1000)), p_VendorId, p_ScoreChange, v_CurrentScore, v_NewScore, p_Reason, p_BookingId);
    
    SELECT v_NewScore as NewScore;
END //

DELIMITER ;

-- =====================================================
-- INITIAL DATA SETUP
-- =====================================================

-- Create wallets for existing vendors
INSERT INTO Wallets (Id, OwnerId, OwnerType, Balance, InterestRate)
SELECT 
    CONCAT(Id, '_wallet_', UNIX_TIMESTAMP()) as Id,
    Id as OwnerId,
    'Vendor' as OwnerType,
    COALESCE(WalletBalance, 0) as Balance,
    4.00 as InterestRate
FROM Vendors
WHERE NOT EXISTS (
    SELECT 1 FROM Wallets 
    WHERE OwnerId = Vendors.Id AND OwnerType = 'Vendor'
);

-- Initialize vendor scores for existing vendors
INSERT INTO VendorScores (Id, VendorId, TrustScore)
SELECT 
    CONCAT(Id, '_score_', UNIX_TIMESTAMP()) as Id,
    Id as VendorId,
    100 as TrustScore
FROM Vendors
WHERE NOT EXISTS (
    SELECT 1 FROM VendorScores 
    WHERE VendorId = Vendors.Id
);

-- =====================================================
-- MIGRATION COMPLETE
-- =====================================================

SELECT 'Business Features Migration Completed Successfully!' as Status;

