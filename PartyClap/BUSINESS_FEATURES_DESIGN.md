# Business Features Design Document
## PartyClap Platform - Comprehensive Feature Design

---

## 1. Incentive Plan for Vendors

### Overview
Multi-tier incentive system to reward vendors based on performance, volume, and quality.

### Incentive Tiers

#### Tier 1: Bronze (0-10 bookings/month)
- **Commission**: 10% platform fee
- **Incentive**: 0% bonus
- **Benefits**: Standard listing

#### Tier 2: Silver (11-25 bookings/month)
- **Commission**: 9% platform fee
- **Incentive**: 1% cashback on total earnings
- **Benefits**: Featured listing, priority support

#### Tier 3: Gold (26-50 bookings/month)
- **Commission**: 8% platform fee
- **Incentive**: 2% cashback + ₹500 monthly bonus
- **Benefits**: Top placement, marketing support

#### Tier 4: Platinum (51+ bookings/month)
- **Commission**: 7% platform fee
- **Incentive**: 3% cashback + ₹1,000 monthly bonus + performance bonus
- **Benefits**: Premium placement, dedicated account manager

### Implementation

#### Database Schema
```sql
CREATE TABLE VendorIncentives (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50),
    MonthYear VARCHAR(7), -- Format: YYYY-MM
    TotalBookings INT DEFAULT 0,
    TotalEarnings DECIMAL(18,2) DEFAULT 0,
    Tier VARCHAR(20) DEFAULT 'Bronze', -- Bronze, Silver, Gold, Platinum
    CommissionRate DECIMAL(5,2) DEFAULT 10.00,
    CashbackAmount DECIMAL(18,2) DEFAULT 0,
    BonusAmount DECIMAL(18,2) DEFAULT 0,
    TotalIncentive DECIMAL(18,2) DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Processed, Paid
    ProcessedDate DATETIME,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id),
    UNIQUE KEY unique_vendor_month (VendorId, MonthYear)
);

CREATE TABLE IncentiveTransactions (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50),
    IncentiveId VARCHAR(50),
    TransactionType VARCHAR(20), -- Cashback, Bonus, TierUpgrade
    Amount DECIMAL(18,2),
    Description TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id),
    FOREIGN KEY (IncentiveId) REFERENCES VendorIncentives(Id)
);
```

#### Business Logic
- Calculate tier at end of each month based on booking count
- Apply commission rate based on tier for next month
- Calculate and credit incentives at month-end
- Auto-upgrade/downgrade tiers based on performance

---

## 2. Scoring and Reward System

### Vendor Scoring Components

#### Trust Score (0-1000 points)
- **Base Score**: 100 points
- **Booking Completion**: +10 points per completed booking
- **Customer Rating**: +5 points per 5-star, +3 per 4-star, +1 per 3-star
- **On-Time Performance**: +5 points for on-time arrival
- **Response Time**: +2 points for responding within 1 hour
- **Cancellation**: -20 points per vendor cancellation
- **Complaints**: -50 points per verified complaint
- **No-Show**: -100 points per no-show

#### Reward Levels
- **New Vendor**: 0-200 points
- **Trusted**: 201-400 points (Badge: 🟢)
- **Verified**: 401-600 points (Badge: 🔵)
- **Premium**: 601-800 points (Badge: 🟣)
- **Elite**: 801-1000 points (Badge: ⭐)

### Customer Scoring
- **Loyalty Points**: 1 point per ₹100 spent
- **Referral Bonus**: 100 points per successful referral
- **Review Points**: 10 points per review submitted
- **Early Booking**: 5 points for booking 30+ days in advance

### Implementation

#### Database Schema
```sql
CREATE TABLE VendorScores (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50),
    TrustScore INT DEFAULT 100,
    TotalBookings INT DEFAULT 0,
    CompletedBookings INT DEFAULT 0,
    CancelledBookings INT DEFAULT 0,
    AverageRating DECIMAL(3,2) DEFAULT 0,
    TotalRatings INT DEFAULT 0,
    OnTimePercentage DECIMAL(5,2) DEFAULT 100,
    ResponseTimeMinutes INT DEFAULT 0,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id)
);

CREATE TABLE ScoreHistory (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50),
    ScoreChange INT, -- Positive or negative
    Reason VARCHAR(100), -- BookingCompleted, RatingReceived, Cancellation, etc.
    BookingId VARCHAR(50),
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id),
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id)
);

CREATE TABLE CustomerRewards (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50),
    LoyaltyPoints INT DEFAULT 0,
    TotalSpent DECIMAL(18,2) DEFAULT 0,
    ReferralCount INT DEFAULT 0,
    ReviewCount INT DEFAULT 0,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE RewardTransactions (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50),
    Points INT,
    TransactionType VARCHAR(50), -- Purchase, Referral, Review, Redemption
    Description TEXT,
    BookingId VARCHAR(50),
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id)
);
```

#### Scoring Rules
- Auto-calculate after each booking completion
- Update trust score in real-time
- Display badges on vendor profiles
- Show loyalty points in customer dashboard

---

## 3. Customer Reward System

### Reward Types

#### Points-Based Rewards
- **Earn**: 1 point per ₹100 spent
- **Redeem**: 
  - 100 points = ₹50 discount
  - 500 points = ₹300 discount
  - 1000 points = ₹700 discount
  - 2000 points = ₹1,500 discount

#### Cashback Program
- **First Booking**: 5% cashback (max ₹500)
- **Regular Booking**: 2% cashback
- **Premium Customer** (10+ bookings): 3% cashback

#### Referral Program
- **Referrer**: ₹200 credit + 100 points
- **Referred**: ₹100 discount on first booking

#### Special Offers
- **Birthday Bonus**: 500 points on birthday month
- **Anniversary**: 300 points
- **Early Bird**: 5% discount for 30+ days advance booking

### Implementation

#### Database Schema
```sql
CREATE TABLE CustomerRewards (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50),
    LoyaltyPoints INT DEFAULT 0,
    TotalSpent DECIMAL(18,2) DEFAULT 0,
    CashbackBalance DECIMAL(18,2) DEFAULT 0,
    ReferralCode VARCHAR(20) UNIQUE,
    ReferralCount INT DEFAULT 0,
    ReviewCount INT DEFAULT 0,
    BirthdayMonth INT,
    AnniversaryDate DATE,
    CustomerTier VARCHAR(20) DEFAULT 'Regular', -- Regular, Premium, VIP
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE RewardRedemptions (
    Id VARCHAR(50) PRIMARY KEY,
    CustomerId VARCHAR(50),
    RewardType VARCHAR(50), -- Points, Cashback, Referral
    PointsUsed INT DEFAULT 0,
    CashbackUsed DECIMAL(18,2) DEFAULT 0,
    DiscountAmount DECIMAL(18,2),
    BookingId VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'Applied', -- Applied, Used, Expired
    ExpiryDate DATETIME,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id)
);
```

---

## 4. Cancellation Handling

### Cancellation Policies

#### Customer Cancellation
- **30+ days before event**: Full refund (minus 5% processing fee)
- **15-29 days before**: 80% refund
- **7-14 days before**: 50% refund
- **Less than 7 days**: No refund (can reschedule once)

#### Vendor Cancellation
- **30+ days before**: Full refund + alternative vendor offered
- **15-29 days before**: Full refund + 10% compensation
- **7-14 days before**: Full refund + 20% compensation
- **Less than 7 days**: Full refund + 30% compensation + priority rebooking

### Cancellation Reasons Tracking
- Customer: Change of plans, Found alternative, Price concern, Other
- Vendor: Emergency, Overbooked, Personal reason, Other

### Implementation

#### Database Schema
```sql
CREATE TABLE Cancellations (
    Id VARCHAR(50) PRIMARY KEY,
    BookingId VARCHAR(50),
    CancelledBy VARCHAR(20), -- Customer, Vendor, Admin
    CancelledById VARCHAR(50), -- CustomerId or VendorId
    CancellationReason VARCHAR(100),
    CancellationDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    RefundAmount DECIMAL(18,2),
    CompensationAmount DECIMAL(18,2),
    RefundStatus VARCHAR(20) DEFAULT 'Pending', -- Pending, Processed, Failed
    RefundProcessedDate DATETIME,
    RefundMethod VARCHAR(20), -- Original, Wallet, Bank
    Notes TEXT,
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id)
);

ALTER TABLE Bookings ADD COLUMN CancellationDate DATETIME;
ALTER TABLE Bookings ADD COLUMN CancelledBy VARCHAR(20);
ALTER TABLE Bookings ADD COLUMN CancellationReason VARCHAR(100);
ALTER TABLE Bookings ADD COLUMN RefundAmount DECIMAL(18,2);
ALTER TABLE Bookings ADD COLUMN CompensationAmount DECIMAL(18,2);
```

#### Business Logic
- Auto-calculate refund based on cancellation date
- Apply vendor penalties for late cancellations
- Update vendor trust score on cancellation
- Send notifications to both parties
- Process refunds within 5-7 business days

---

## 5. Insurance for Vendors

### Insurance Coverage

#### Coverage Types
- **Public Liability**: ₹10 Lakhs coverage
- **Equipment Insurance**: Vendor's equipment covered
- **Professional Indemnity**: Errors & omissions coverage
- **Personal Accident**: ₹5 Lakhs coverage

#### Insurance Requirements
- **Mandatory for**: All registered vendors
- **Premium**: ₹500/month (deducted from wallet)
- **Coverage Period**: Monthly renewable
- **Claims Process**: Online claim submission, 15-day processing

### Implementation

#### Database Schema
```sql
CREATE TABLE VendorInsurance (
    Id VARCHAR(50) PRIMARY KEY,
    VendorId VARCHAR(50),
    PolicyNumber VARCHAR(50) UNIQUE,
    InsuranceType VARCHAR(50), -- PublicLiability, Equipment, ProfessionalIndemnity, PersonalAccident
    CoverageAmount DECIMAL(18,2),
    PremiumAmount DECIMAL(18,2),
    StartDate DATE,
    EndDate DATE,
    Status VARCHAR(20) DEFAULT 'Active', -- Active, Expired, Cancelled
    AutoRenew BOOLEAN DEFAULT TRUE,
    LastPaymentDate DATETIME,
    NextPaymentDate DATETIME,
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id)
);

CREATE TABLE InsuranceClaims (
    Id VARCHAR(50) PRIMARY KEY,
    InsuranceId VARCHAR(50),
    VendorId VARCHAR(50),
    BookingId VARCHAR(50),
    ClaimType VARCHAR(50),
    ClaimAmount DECIMAL(18,2),
    IncidentDate DATETIME,
    Description TEXT,
    SupportingDocuments TEXT, -- JSON array of file URLs
    Status VARCHAR(20) DEFAULT 'Submitted', -- Submitted, UnderReview, Approved, Rejected
    ApprovedAmount DECIMAL(18,2),
    ProcessedDate DATETIME,
    Notes TEXT,
    FOREIGN KEY (InsuranceId) REFERENCES VendorInsurance(Id),
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id),
    FOREIGN KEY (BookingId) REFERENCES Bookings(Id)
);

CREATE TABLE InsurancePayments (
    Id VARCHAR(50) PRIMARY KEY,
    InsuranceId VARCHAR(50),
    VendorId VARCHAR(50),
    Amount DECIMAL(18,2),
    PaymentDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    PaymentMethod VARCHAR(20), -- Wallet, Bank, UPI
    Status VARCHAR(20) DEFAULT 'Completed',
    FOREIGN KEY (InsuranceId) REFERENCES VendorInsurance(Id),
    FOREIGN KEY (VendorId) REFERENCES Vendors(Id)
);
```

#### Business Logic
- Auto-deduct premium from vendor wallet monthly
- Suspend vendor if insurance expires
- Track claims and payout history
- Send renewal reminders 7 days before expiry

---

## 6. Professional Indemnity for Platform

### Coverage Details
- **Coverage Amount**: ₹50 Crores
- **Coverage Type**: Platform liability, data breach, service failures
- **Premium**: Business expense (not passed to vendors)
- **Renewal**: Annual

### Implementation

#### Database Schema
```sql
CREATE TABLE PlatformInsurance (
    Id VARCHAR(50) PRIMARY KEY,
    PolicyNumber VARCHAR(50) UNIQUE,
    InsuranceProvider VARCHAR(100),
    CoverageAmount DECIMAL(18,2),
    PremiumAmount DECIMAL(18,2),
    StartDate DATE,
    EndDate DATE,
    Status VARCHAR(20) DEFAULT 'Active',
    ContactPerson VARCHAR(100),
    ContactEmail VARCHAR(100),
    ContactPhone VARCHAR(20),
    Documents TEXT, -- JSON array of policy documents
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE PlatformClaims (
    Id VARCHAR(50) PRIMARY KEY,
    InsuranceId VARCHAR(50),
    ClaimType VARCHAR(50), -- Liability, DataBreach, ServiceFailure
    IncidentDate DATETIME,
    Description TEXT,
    ClaimAmount DECIMAL(18,2),
    AffectedParties INT, -- Number of customers/vendors affected
    Status VARCHAR(20) DEFAULT 'Submitted',
    ApprovedAmount DECIMAL(18,2),
    ProcessedDate DATETIME,
    Notes TEXT,
    FOREIGN KEY (InsuranceId) REFERENCES PlatformInsurance(Id)
);
```

#### Business Logic
- Track all incidents that may lead to claims
- Maintain documentation for all claims
- Annual renewal reminder system
- Compliance reporting

---

## 7. Wallet for Earning Interest

### Interest-Bearing Wallet

#### Features
- **Interest Rate**: 4% per annum (compounded monthly)
- **Minimum Balance**: ₹1,000 to earn interest
- **Interest Calculation**: Daily balance, monthly credit
- **Withdrawal**: Instant (UPI) or 24 hours (Bank transfer)

#### Interest Calculation
- Daily Average Balance × (4% / 365) = Daily Interest
- Monthly Interest = Sum of Daily Interest
- Credited on 1st of each month

### Implementation

#### Database Schema
```sql
CREATE TABLE Wallets (
    Id VARCHAR(50) PRIMARY KEY,
    OwnerId VARCHAR(50), -- VendorId or CustomerId
    OwnerType VARCHAR(20), -- Vendor, Customer
    Balance DECIMAL(18,2) DEFAULT 0,
    LockedBalance DECIMAL(18,2) DEFAULT 0, -- For pending transactions
    InterestRate DECIMAL(5,2) DEFAULT 4.00,
    LastInterestDate DATE,
    TotalInterestEarned DECIMAL(18,2) DEFAULT 0,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY unique_owner (OwnerId, OwnerType)
);

CREATE TABLE WalletTransactions (
    Id VARCHAR(50) PRIMARY KEY,
    WalletId VARCHAR(50),
    TransactionType VARCHAR(50), -- Credit, Debit, Interest, Refund, Withdrawal
    Amount DECIMAL(18,2),
    BalanceBefore DECIMAL(18,2),
    BalanceAfter DECIMAL(18,2),
    Description TEXT,
    ReferenceId VARCHAR(50), -- BookingId, IncentiveId, etc.
    ReferenceType VARCHAR(50), -- Booking, Incentive, Refund, etc.
    Status VARCHAR(20) DEFAULT 'Completed', -- Pending, Completed, Failed
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (WalletId) REFERENCES Wallets(Id)
);

CREATE TABLE InterestCalculations (
    Id VARCHAR(50) PRIMARY KEY,
    WalletId VARCHAR(50),
    MonthYear VARCHAR(7), -- Format: YYYY-MM
    AverageDailyBalance DECIMAL(18,2),
    InterestRate DECIMAL(5,2),
    InterestAmount DECIMAL(18,2),
    DaysInMonth INT,
    CalculatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    CreditedDate DATETIME,
    Status VARCHAR(20) DEFAULT 'Calculated', -- Calculated, Credited, Failed
    FOREIGN KEY (WalletId) REFERENCES Wallets(Id),
    UNIQUE KEY unique_wallet_month (WalletId, MonthYear)
);

CREATE TABLE Withdrawals (
    Id VARCHAR(50) PRIMARY KEY,
    WalletId VARCHAR(50),
    Amount DECIMAL(18,2),
    WithdrawalMethod VARCHAR(20), -- UPI, Bank, Wallet
    UpiId VARCHAR(100),
    BankAccountNumber VARCHAR(50),
    IfscCode VARCHAR(20),
    AccountHolderName VARCHAR(100),
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Processing, Completed, Failed
    ProcessedDate DATETIME,
    TransactionId VARCHAR(100),
    FailureReason TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (WalletId) REFERENCES Wallets(Id)
);
```

#### Business Logic
- Auto-create wallet on vendor/customer registration
- Calculate interest daily based on average balance
- Credit interest on 1st of each month
- Minimum balance check before interest calculation
- Withdrawal limits: ₹10,000/day for vendors, ₹5,000/day for customers

---

## Implementation Priority

### Phase 1 (MVP - 2 weeks)
1. ✅ Basic wallet system
2. ✅ Cancellation handling
3. ✅ Basic scoring system

### Phase 2 (1 month)
4. ✅ Incentive plan
5. ✅ Customer rewards
6. ✅ Interest-bearing wallet

### Phase 3 (2 months)
7. ✅ Vendor insurance
8. ✅ Professional indemnity
9. ✅ Advanced scoring and rewards

---

## API Endpoints Required

### Wallet APIs
- `GET /api/wallet/balance` - Get wallet balance
- `POST /api/wallet/withdraw` - Request withdrawal
- `GET /api/wallet/transactions` - Transaction history
- `GET /api/wallet/interest` - Interest calculation history

### Incentive APIs
- `GET /api/incentives/current` - Current tier and incentives
- `GET /api/incentives/history` - Historical incentive data
- `GET /api/incentives/tier` - Tier information

### Scoring APIs
- `GET /api/scores/vendor/{id}` - Vendor score details
- `GET /api/scores/customer/{id}` - Customer loyalty points
- `POST /api/scores/update` - Update score (admin)

### Cancellation APIs
- `POST /api/bookings/{id}/cancel` - Cancel booking
- `GET /api/cancellations` - Cancellation history
- `POST /api/cancellations/{id}/refund` - Process refund

### Insurance APIs
- `GET /api/insurance/vendor` - Vendor insurance status
- `POST /api/insurance/claim` - Submit insurance claim
- `GET /api/insurance/platform` - Platform insurance details

---

## Notifications Required

### Vendor Notifications
- Tier upgrade/downgrade
- Incentive credited
- Insurance renewal reminder
- Score update
- Interest credited

### Customer Notifications
- Points earned
- Cashback credited
- Reward redemption confirmation
- Cancellation refund processed
- Interest credited

---

## Reporting & Analytics

### Vendor Dashboard
- Earnings summary
- Incentive breakdown
- Score trends
- Insurance status
- Wallet balance and interest

### Customer Dashboard
- Loyalty points
- Cashback balance
- Reward redemption history
- Wallet balance and interest

### Admin Dashboard
- Platform insurance status
- Total incentives paid
- Cancellation statistics
- Insurance claims tracking
- Interest payouts

---

## Security & Compliance

### Data Protection
- Encrypt sensitive financial data
- PCI-DSS compliance for payment processing
- Regular security audits

### Financial Compliance
- Maintain audit trail for all transactions
- Regular reconciliation
- Tax reporting (TDS on interest > ₹40,000/year)
- GST compliance

---

## Testing Requirements

### Unit Tests
- Score calculation logic
- Interest calculation
- Incentive tier determination
- Refund calculation

### Integration Tests
- Wallet transactions
- Insurance premium deduction
- Cancellation workflow
- Interest crediting

### User Acceptance Tests
- End-to-end incentive flow
- Cancellation and refund process
- Insurance claim submission
- Wallet withdrawal

---

## Documentation Required

1. **User Guides**
   - How to earn and redeem rewards
   - How to manage wallet
   - How to submit insurance claims

2. **Vendor Guides**
   - Understanding incentive tiers
   - Improving trust score
   - Insurance requirements

3. **Admin Guides**
   - Managing incentives
   - Processing refunds
   - Insurance management

---

## Next Steps

1. Review and approve this design document
2. Create detailed database migration scripts
3. Implement Phase 1 features
4. Set up testing environment
5. Deploy to staging for UAT
6. Production deployment

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-27  
**Status**: Draft for Review

