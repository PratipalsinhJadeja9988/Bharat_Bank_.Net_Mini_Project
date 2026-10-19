-- =============================================
-- BharatTrust Bank - secure banking system
-- Database Creation & Schema Script for PostgreSQL
-- =============================================

-- 1. System Settings Table
DROP TABLE IF EXISTS SystemSettings CASCADE;
CREATE TABLE SystemSettings (
    SettingKey VARCHAR(50) PRIMARY KEY,
    SettingValue VARCHAR(255) NOT NULL,
    Description VARCHAR(255) NULL,
    LastUpdated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2. Users Table (Core Auth for all roles)
DROP TABLE IF EXISTS Users CASCADE;
CREATE TABLE Users (
    UserId SERIAL PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(256) NOT NULL,
    Salt VARCHAR(50) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    MobileNumber VARCHAR(15) NOT NULL,
    IsActive INTEGER DEFAULT 1,
    IsLocked INTEGER DEFAULT 0,
    FailedLoginAttempts INT DEFAULT 0,
    LockoutEnd TIMESTAMP NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    LastLoginAt TIMESTAMP NULL
);

-- 3. Customers Table
DROP TABLE IF EXISTS Customers CASCADE;
CREATE TABLE Customers (
    CustomerId SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    CustomerCode VARCHAR(20) NOT NULL UNIQUE,
    DateOfBirth DATE NOT NULL,
    Gender VARCHAR(10) NOT NULL,
    Address VARCHAR(255) NOT NULL,
    City VARCHAR(50) NOT NULL,
    State VARCHAR(50) NOT NULL,
    Pincode VARCHAR(10) NOT NULL,
    PAN VARCHAR(20) NOT NULL,
    Aadhaar VARCHAR(20) NOT NULL,
    KYCStatus VARCHAR(20) DEFAULT 'Pending',
    KYCVerifiedBy INT NULL,
    KYCVerifiedDate TIMESTAMP NULL,
    KYCRemarks VARCHAR(255) NULL,
    NomineeName VARCHAR(100) NULL,
    NomineeRelation VARCHAR(50) NULL,
    Segment VARCHAR(30) DEFAULT 'Regular Customer',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 4. Employees Table
DROP TABLE IF EXISTS Employees CASCADE;
CREATE TABLE Employees (
    EmployeeId SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    EmployeeCode VARCHAR(20) NOT NULL UNIQUE,
    Department VARCHAR(50) NOT NULL,
    Designation VARCHAR(50) NOT NULL,
    BranchName VARCHAR(100) DEFAULT 'Main Branch - Mumbai',
    DateOfJoining DATE NOT NULL,
    Salary DECIMAL(18,2) DEFAULT 45000.00
);

-- 5. Account Types Table
DROP TABLE IF EXISTS AccountTypes CASCADE;
CREATE TABLE AccountTypes (
    AccountTypeId SERIAL PRIMARY KEY,
    TypeName VARCHAR(50) NOT NULL,
    InterestRate DECIMAL(5,2) DEFAULT 4.00,
    MinimumBalance DECIMAL(18,2) DEFAULT 1000.00,
    Description VARCHAR(255) NULL
);

-- 6. Accounts Table
DROP TABLE IF EXISTS Accounts CASCADE;
CREATE TABLE Accounts (
    AccountId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountNumber VARCHAR(20) NOT NULL UNIQUE,
    AccountTypeId INTEGER NOT NULL REFERENCES AccountTypes(AccountTypeId),
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Currency VARCHAR(5) DEFAULT 'INR',
    Status VARCHAR(20) DEFAULT 'Active',
    IFSCCode VARCHAR(20) DEFAULT 'BHTT0001001',
    BranchName VARCHAR(100) DEFAULT 'Main Branch, Nariman Point, Mumbai',
    OpenedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    LastActivityDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 7. Beneficiaries Table
DROP TABLE IF EXISTS Beneficiaries CASCADE;
CREATE TABLE Beneficiaries (
    BeneficiaryId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    BeneficiaryName VARCHAR(100) NOT NULL,
    AccountNumber VARCHAR(20) NOT NULL,
    BankName VARCHAR(100) NOT NULL,
    IFSCCode VARCHAR(20) NOT NULL,
    TransferType VARCHAR(10) DEFAULT 'IMPS',
    UPIId VARCHAR(50) NULL,
    MaxLimit DECIMAL(18,2) DEFAULT 50000.00,
    IsActive INTEGER DEFAULT 1,
    AddedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 8. Transactions Table
DROP TABLE IF EXISTS Transactions CASCADE;
CREATE TABLE Transactions (
    TransactionId SERIAL PRIMARY KEY,
    TransactionReference VARCHAR(30) NOT NULL UNIQUE,
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    TransactionType VARCHAR(20) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    BalanceAfter DECIMAL(18,2) NOT NULL,
    DebitOrCredit VARCHAR(10) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Description VARCHAR(255) NOT NULL,
    DestinationAccount VARCHAR(50) NULL,
    BeneficiaryName VARCHAR(100) NULL,
    PaymentMethod VARCHAR(20) DEFAULT 'Online',
    Status VARCHAR(20) DEFAULT 'Completed',
    TransactionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 9. Scheduled Transfers Table
DROP TABLE IF EXISTS ScheduledTransfers CASCADE;
CREATE TABLE ScheduledTransfers (
    ScheduleId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    FromAccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    ToAccountNumber VARCHAR(20) NOT NULL,
    BeneficiaryName VARCHAR(100) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransferType VARCHAR(10) NOT NULL,
    Frequency VARCHAR(20) NOT NULL,
    ScheduledDate DATE NOT NULL,
    Status VARCHAR(20) DEFAULT 'Pending',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 10. Fixed Deposits (FD) Table
DROP TABLE IF EXISTS FixedDeposits CASCADE;
CREATE TABLE FixedDeposits (
    FDId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    FDNumber VARCHAR(20) NOT NULL UNIQUE,
    PrincipalAmount DECIMAL(18,2) NOT NULL,
    TenureMonths INT NOT NULL,
    InterestRate DECIMAL(5,2) NOT NULL,
    InterestEarned DECIMAL(18,2) NOT NULL,
    MaturityAmount DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL,
    MaturityDate DATE NOT NULL,
    Status VARCHAR(20) DEFAULT 'Active',
    AutoRenew INTEGER DEFAULT 0,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 11. Recurring Deposits (RD) Table
DROP TABLE IF EXISTS RecurringDeposits CASCADE;
CREATE TABLE RecurringDeposits (
    RDId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    RDNumber VARCHAR(20) NOT NULL UNIQUE,
    MonthlyInstallment DECIMAL(18,2) NOT NULL,
    TenureMonths INT NOT NULL,
    InterestRate DECIMAL(5,2) NOT NULL,
    TotalDeposited DECIMAL(18,2) NOT NULL,
    MaturityAmount DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL,
    MaturityDate DATE NOT NULL,
    InstallmentsPaid INT DEFAULT 1,
    Status VARCHAR(20) DEFAULT 'Active',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 12. Loans Table
DROP TABLE IF EXISTS Loans CASCADE;
CREATE TABLE Loans (
    LoanId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    LoanType VARCHAR(50) NOT NULL,
    LoanAmount DECIMAL(18,2) NOT NULL,
    TenureMonths INT NOT NULL,
    InterestRate DECIMAL(5,2) NOT NULL,
    MonthlyEMI DECIMAL(18,2) NOT NULL,
    TotalRepayment DECIMAL(18,2) NOT NULL,
    MonthlyIncome DECIMAL(18,2) NOT NULL,
    EmploymentType VARCHAR(50) NOT NULL,
    Purpose VARCHAR(255) NOT NULL,
    CreditScore INT DEFAULT 720,
    Status VARCHAR(20) DEFAULT 'Pending',
    Remarks VARCHAR(255) NULL,
    ApprovedBy INT NULL,
    ApprovedDate TIMESTAMP NULL,
    DisbursedDate TIMESTAMP NULL,
    OutstandingBalance DECIMAL(18,2) DEFAULT 0.00,
    NextEMIDate DATE NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 13. Loan Payments Table
DROP TABLE IF EXISTS LoanPayments CASCADE;
CREATE TABLE LoanPayments (
    PaymentId SERIAL PRIMARY KEY,
    LoanId INTEGER NOT NULL REFERENCES Loans(LoanId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    PaymentDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    Amount DECIMAL(18,2) NOT NULL,
    PrincipalComponent DECIMAL(18,2) NOT NULL,
    InterestComponent DECIMAL(18,2) NOT NULL,
    RemainingBalance DECIMAL(18,2) NOT NULL,
    PaymentReference VARCHAR(50) NOT NULL
);

-- 14. Debit Cards Table
DROP TABLE IF EXISTS DebitCards CASCADE;
CREATE TABLE DebitCards (
    DebitCardId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    CardNumber VARCHAR(20) NOT NULL UNIQUE,
    CardHolderName VARCHAR(100) NOT NULL,
    CardType VARCHAR(20) DEFAULT 'RuPay Platinum',
    ExpiryDate VARCHAR(10) NOT NULL,
    CVV VARCHAR(10) NOT NULL,
    PINHash VARCHAR(256) NOT NULL,
    DailyLimit DECIMAL(18,2) DEFAULT 50000.00,
    IsInternationalEnabled INTEGER DEFAULT 0,
    IsOnlineEnabled INTEGER DEFAULT 1,
    IsContactlessEnabled INTEGER DEFAULT 1,
    Status VARCHAR(20) DEFAULT 'Active',
    IssuedDate DATE DEFAULT CURRENT_DATE
);

-- 15. Credit Cards Table
DROP TABLE IF EXISTS CreditCards CASCADE;
CREATE TABLE CreditCards (
    CreditCardId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    CardNumber VARCHAR(20) NOT NULL UNIQUE,
    CardHolderName VARCHAR(100) NOT NULL,
    CardType VARCHAR(50) DEFAULT 'BharatTrust Regalia',
    CreditLimit DECIMAL(18,2) DEFAULT 150000.00,
    AvailableLimit DECIMAL(18,2) DEFAULT 150000.00,
    ExpiryDate VARCHAR(10) NOT NULL,
    CVV VARCHAR(10) NOT NULL,
    PINHash VARCHAR(256) NOT NULL,
    RewardPoints INT DEFAULT 500,
    IsInternationalEnabled INTEGER DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'Active',
    BillingCycleDay INT DEFAULT 15,
    IssuedDate DATE DEFAULT CURRENT_DATE
);

-- 16. Cheque Requests Table
DROP TABLE IF EXISTS ChequeRequests CASCADE;
CREATE TABLE ChequeRequests (
    RequestId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    NumberOfLeaves INT NOT NULL,
    DeliveryAddress VARCHAR(255) NOT NULL,
    Status VARCHAR(20) DEFAULT 'Pending',
    Remarks VARCHAR(255) NULL,
    ProcessedBy INT NULL,
    RequestDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ProcessedDate TIMESTAMP NULL
);

-- 17. Service Requests Table
DROP TABLE IF EXISTS ServiceRequests CASCADE;
CREATE TABLE ServiceRequests (
    ServiceRequestId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    RequestType VARCHAR(50) NOT NULL,
    OldValue VARCHAR(255) NULL,
    NewValue VARCHAR(255) NOT NULL,
    Reason VARCHAR(255) NULL,
    Status VARCHAR(20) DEFAULT 'Pending',
    Remarks VARCHAR(255) NULL,
    ProcessedBy INT NULL,
    RequestDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ProcessedDate TIMESTAMP NULL
);

-- 18. Bill Payments Table
DROP TABLE IF EXISTS BillPayments CASCADE;
CREATE TABLE BillPayments (
    BillPaymentId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    BillerCategory VARCHAR(50) NOT NULL,
    BillerName VARCHAR(100) NOT NULL,
    ConsumerNumber VARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionReference VARCHAR(50) NOT NULL,
    Status VARCHAR(20) DEFAULT 'Success',
    PaymentDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 19. Investments Table
DROP TABLE IF EXISTS Investments CASCADE;
CREATE TABLE Investments (
    InvestmentId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    InvestmentType VARCHAR(50) NOT NULL,
    SchemeName VARCHAR(100) NOT NULL,
    InvestedAmount DECIMAL(18,2) NOT NULL,
    CurrentValue DECIMAL(18,2) NOT NULL,
    Units DECIMAL(18,4) DEFAULT 1.0,
    NAV DECIMAL(18,2) DEFAULT 100.0,
    ReturnsPercent DECIMAL(5,2) DEFAULT 0.0,
    Status VARCHAR(20) DEFAULT 'Active',
    InvestmentDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 20. Support Tickets Table
DROP TABLE IF EXISTS SupportTickets CASCADE;
CREATE TABLE SupportTickets (
    TicketId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    TicketNumber VARCHAR(20) NOT NULL UNIQUE,
    Subject VARCHAR(100) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Message TEXT NOT NULL,
    Priority VARCHAR(20) DEFAULT 'Normal',
    Status VARCHAR(20) DEFAULT 'Open',
    AssignedEmployeeId INT NULL,
    Resolution TEXT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ResolvedAt TIMESTAMP NULL
);

-- 21. Live Chat / Messages Table
DROP TABLE IF EXISTS ChatMessages CASCADE;
CREATE TABLE ChatMessages (
    MessageId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    Sender VARCHAR(20) NOT NULL,
    SenderName VARCHAR(100) NOT NULL,
    Message TEXT NOT NULL,
    IsRead INTEGER DEFAULT 0,
    SentAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 22. Notifications Table
DROP TABLE IF EXISTS Notifications CASCADE;
CREATE TABLE Notifications (
    NotificationId SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    Title VARCHAR(100) NOT NULL,
    Message VARCHAR(255) NOT NULL,
    NotificationType VARCHAR(30) DEFAULT 'General',
    IsRead INTEGER DEFAULT 0,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 23. Login History & Devices Table
DROP TABLE IF EXISTS LoginHistory CASCADE;
CREATE TABLE LoginHistory (
    LoginId SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    IPAddress VARCHAR(50) DEFAULT '127.0.0.1',
    Browser VARCHAR(100) DEFAULT 'Chrome / Windows',
    DeviceType VARCHAR(50) DEFAULT 'Desktop PC',
    IsSuccess INTEGER NOT NULL,
    FailureReason VARCHAR(100) NULL,
    LoginTime TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 24. Devices Table
DROP TABLE IF EXISTS Devices CASCADE;
CREATE TABLE Devices (
    DeviceId SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    DeviceName VARCHAR(100) NOT NULL,
    DeviceType VARCHAR(50) NOT NULL,
    Browser VARCHAR(100) NOT NULL,
    LastIpAddress VARCHAR(50) NOT NULL,
    IsTrusted INTEGER DEFAULT 1,
    FirstSeen TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    LastActive TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 25. Security Questions Table
DROP TABLE IF EXISTS SecurityQuestions CASCADE;
CREATE TABLE SecurityQuestions (
    SecurityQuestionId SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    Question VARCHAR(255) NOT NULL,
    AnswerHash VARCHAR(256) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 26. Fraud Alerts Table
DROP TABLE IF EXISTS FraudAlerts CASCADE;
CREATE TABLE FraudAlerts (
    AlertId SERIAL PRIMARY KEY,
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    TransactionId INT NULL,
    TriggerRule VARCHAR(100) NOT NULL,
    RiskLevel VARCHAR(20) DEFAULT 'Medium',
    Description VARCHAR(255) NOT NULL,
    Status VARCHAR(20) DEFAULT 'New',
    ReviewedBy INT NULL,
    ReviewRemarks VARCHAR(255) NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ReviewedAt TIMESTAMP NULL
);

-- 27. Audit & Activity Logs Table
DROP TABLE IF EXISTS AuditLogs CASCADE;
CREATE TABLE AuditLogs (
    LogId SERIAL PRIMARY KEY,
    UserId INT NULL,
    Username VARCHAR(50) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    Action VARCHAR(50) NOT NULL,
    Description VARCHAR(500) NOT NULL,
    IPAddress VARCHAR(50) DEFAULT '127.0.0.1',
    Timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 28. Dynamic Advertisements / Banners Table
DROP TABLE IF EXISTS Advertisements CASCADE;
CREATE TABLE Advertisements (
    AdId SERIAL PRIMARY KEY,
    Title VARCHAR(100) NOT NULL,
    Subtitle VARCHAR(255) NOT NULL,
    TagText VARCHAR(50) DEFAULT 'Special Offer',
    BadgeColor VARCHAR(30) DEFAULT 'bg-primary',
    CallToActionText VARCHAR(50) DEFAULT 'Apply Now',
    TargetUrl VARCHAR(255) DEFAULT '/Customer/Loans/Apply',
    ImageUrl VARCHAR(255) DEFAULT '/images/banner-loan.jpg',
    DisplayOrder INT DEFAULT 1,
    IsActive INTEGER DEFAULT 1,
    StartDate DATE DEFAULT CURRENT_DATE,
    EndDate DATE DEFAULT (CURRENT_DATE + INTERVAL '1 year'),
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 29. Dynamic FAQs Table
DROP TABLE IF EXISTS FAQs CASCADE;
CREATE TABLE FAQs (
    FAQId SERIAL PRIMARY KEY,
    Category VARCHAR(50) NOT NULL,
    Question VARCHAR(255) NOT NULL,
    Answer TEXT NOT NULL,
    DisplayOrder INT DEFAULT 1,
    IsActive INTEGER DEFAULT 1
);

-- 30. Dynamic Offers Table
DROP TABLE IF EXISTS Offers CASCADE;
CREATE TABLE Offers (
    OfferId SERIAL PRIMARY KEY,
    Title VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NOT NULL,
    Category VARCHAR(50) NOT NULL,
    DiscountCode VARCHAR(50) NULL,
    ValidTill DATE NOT NULL,
    IsActive INTEGER DEFAULT 1
);

-- 31. Dynamic Currency Exchange Rates Table
DROP TABLE IF EXISTS CurrencyRates CASCADE;
CREATE TABLE CurrencyRates (
    CurrencyId SERIAL PRIMARY KEY,
    CurrencyCode VARCHAR(5) NOT NULL UNIQUE,
    CurrencyName VARCHAR(50) NOT NULL,
    BuyRate DECIMAL(10,4) NOT NULL,
    SellRate DECIMAL(10,4) NOT NULL,
    LastUpdated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 32. Dynamic Interest Rates Table
DROP TABLE IF EXISTS InterestRates CASCADE;
CREATE TABLE InterestRates (
    RateId SERIAL PRIMARY KEY,
    ProductType VARCHAR(50) NOT NULL,
    TenureOrTier VARCHAR(50) NOT NULL,
    RegularRate DECIMAL(5,2) NOT NULL,
    SeniorCitizenRate DECIMAL(5,2) NOT NULL,
    LastUpdated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 33. Feedback & Complaints Table
DROP TABLE IF EXISTS Feedback CASCADE;
CREATE TABLE Feedback (
    FeedbackId SERIAL PRIMARY KEY,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    Rating INT NOT NULL,
    Category VARCHAR(50) NOT NULL,
    Comments TEXT NOT NULL,
    SubmittedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);



