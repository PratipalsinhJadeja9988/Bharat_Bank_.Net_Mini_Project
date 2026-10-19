-- =============================================
-- BharatTrust Bank - secure banking system
-- Database Creation & Schema Script
-- Target Database: BharatTrustBankDB
-- =============================================

-- 1. System Settings Table
DROP TABLE IF EXISTS SystemSettings;
CREATE TABLE SystemSettings (
    SettingKey NVARCHAR(50) PRIMARY KEY,
    SettingValue NVARCHAR(255) NOT NULL,
    Description NVARCHAR(255) NULL,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 2. Users Table (Core Auth for all roles)
DROP TABLE IF EXISTS Users;
CREATE TABLE Users (
    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Salt NVARCHAR(50) NOT NULL,
    Role NVARCHAR(20) NOT NULL, -- 'Customer', 'Employee', 'Manager', 'Admin'
    FullName NVARCHAR(100) NOT NULL,
    MobileNumber NVARCHAR(15) NOT NULL,
    IsActive INTEGER DEFAULT 1,
    IsLocked INTEGER DEFAULT 0,
    FailedLoginAttempts INT DEFAULT 0,
    LockoutEnd DATETIME NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastLoginAt DATETIME NULL
);

-- 3. Customers Table
DROP TABLE IF EXISTS Customers;
CREATE TABLE Customers (
    CustomerId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    CustomerCode NVARCHAR(20) NOT NULL UNIQUE,
    DateOfBirth DATE NOT NULL,
    Gender NVARCHAR(10) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    City NVARCHAR(50) NOT NULL,
    State NVARCHAR(50) NOT NULL,
    Pincode NVARCHAR(10) NOT NULL,
    PAN NVARCHAR(20) NOT NULL,
    Aadhaar NVARCHAR(20) NOT NULL,
    KYCStatus NVARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Verified', 'Rejected'
    KYCVerifiedBy INT NULL,
    KYCVerifiedDate DATETIME NULL,
    KYCRemarks NVARCHAR(255) NULL,
    NomineeName NVARCHAR(100) NULL,
    NomineeRelation NVARCHAR(50) NULL,
    Segment NVARCHAR(30) DEFAULT 'Regular Customer', -- 'Premium Customer', 'Regular Customer', 'New Customer'
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 4. Employees Table
DROP TABLE IF EXISTS Employees;
CREATE TABLE Employees (
    EmployeeId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    EmployeeCode NVARCHAR(20) NOT NULL UNIQUE,
    Department NVARCHAR(50) NOT NULL,
    Designation NVARCHAR(50) NOT NULL,
    BranchName NVARCHAR(100) DEFAULT 'Main Branch - Mumbai',
    DateOfJoining DATE NOT NULL,
    Salary DECIMAL(18,2) DEFAULT 45000.00
);

-- 5. Account Types Table
DROP TABLE IF EXISTS AccountTypes;
CREATE TABLE AccountTypes (
    AccountTypeId INTEGER PRIMARY KEY AUTOINCREMENT,
    TypeName NVARCHAR(50) NOT NULL, -- 'Savings Account', 'Current Account', 'Salary Account'
    InterestRate DECIMAL(5,2) DEFAULT 4.00,
    MinimumBalance DECIMAL(18,2) DEFAULT 1000.00,
    Description NVARCHAR(255) NULL
);

-- 6. Accounts Table
DROP TABLE IF EXISTS Accounts;
CREATE TABLE Accounts (
    AccountId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountNumber NVARCHAR(20) NOT NULL UNIQUE,
    AccountTypeId INTEGER NOT NULL REFERENCES AccountTypes(AccountTypeId),
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Currency NVARCHAR(5) DEFAULT 'INR',
    Status NVARCHAR(20) DEFAULT 'Active', -- 'Active', 'Inactive', 'Frozen', 'Closed'
    IFSCCode NVARCHAR(20) DEFAULT 'BHTT0001001',
    BranchName NVARCHAR(100) DEFAULT 'Main Branch, Nariman Point, Mumbai',
    OpenedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastActivityDate DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 7. Beneficiaries Table
DROP TABLE IF EXISTS Beneficiaries;
CREATE TABLE Beneficiaries (
    BeneficiaryId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    BeneficiaryName NVARCHAR(100) NOT NULL,
    AccountNumber NVARCHAR(20) NOT NULL,
    BankName NVARCHAR(100) NOT NULL,
    IFSCCode NVARCHAR(20) NOT NULL,
    TransferType NVARCHAR(10) DEFAULT 'IMPS', -- 'IMPS', 'NEFT', 'RTGS', 'UPI'
    UPIId NVARCHAR(50) NULL,
    MaxLimit DECIMAL(18,2) DEFAULT 50000.00,
    IsActive INTEGER DEFAULT 1,
    AddedDate DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 8. Transactions Table
DROP TABLE IF EXISTS Transactions;
CREATE TABLE Transactions (
    TransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
    TransactionReference NVARCHAR(30) NOT NULL UNIQUE,
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    TransactionType NVARCHAR(20) NOT NULL, -- 'Deposit', 'Withdrawal', 'Transfer', 'BillPayment', 'Recharge', 'LoanEMI', 'Investment'
    Amount DECIMAL(18,2) NOT NULL,
    BalanceAfter DECIMAL(18,2) NOT NULL,
    DebitOrCredit NVARCHAR(10) NOT NULL, -- 'Debit', 'Credit'
    Category NVARCHAR(50) NOT NULL, -- 'Transfer', 'Salary', 'Shopping', 'Utilities', 'Investment', 'EMI', 'Cash'
    Description NVARCHAR(255) NOT NULL,
    DestinationAccount NVARCHAR(50) NULL,
    BeneficiaryName NVARCHAR(100) NULL,
    PaymentMethod NVARCHAR(20) DEFAULT 'Online', -- 'IMPS', 'NEFT', 'RTGS', 'UPI', 'QR', 'ATM', 'Card', 'Online'
    Status NVARCHAR(20) DEFAULT 'Completed', -- 'Completed', 'Pending', 'Failed'
    TransactionDate DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 9. Scheduled Transfers Table
DROP TABLE IF EXISTS ScheduledTransfers;
CREATE TABLE ScheduledTransfers (
    ScheduleId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    FromAccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    ToAccountNumber NVARCHAR(20) NOT NULL,
    BeneficiaryName NVARCHAR(100) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransferType NVARCHAR(10) NOT NULL,
    Frequency NVARCHAR(20) NOT NULL, -- 'OneTime', 'Daily', 'Weekly', 'Monthly'
    ScheduledDate DATE NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Executed', 'Cancelled'
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 10. Fixed Deposits (FD) Table
DROP TABLE IF EXISTS FixedDeposits;
CREATE TABLE FixedDeposits (
    FDId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    FDNumber NVARCHAR(20) NOT NULL UNIQUE,
    PrincipalAmount DECIMAL(18,2) NOT NULL,
    TenureMonths INT NOT NULL,
    InterestRate DECIMAL(5,2) NOT NULL,
    InterestEarned DECIMAL(18,2) NOT NULL,
    MaturityAmount DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL,
    MaturityDate DATE NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Active', -- 'Active', 'Matured', 'PrematureClosed'
    AutoRenew INTEGER DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 11. Recurring Deposits (RD) Table
DROP TABLE IF EXISTS RecurringDeposits;
CREATE TABLE RecurringDeposits (
    RDId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    RDNumber NVARCHAR(20) NOT NULL UNIQUE,
    MonthlyInstallment DECIMAL(18,2) NOT NULL,
    TenureMonths INT NOT NULL,
    InterestRate DECIMAL(5,2) NOT NULL,
    TotalDeposited DECIMAL(18,2) NOT NULL,
    MaturityAmount DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL,
    MaturityDate DATE NOT NULL,
    InstallmentsPaid INT DEFAULT 1,
    Status NVARCHAR(20) DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 12. Loans Table
DROP TABLE IF EXISTS Loans;
CREATE TABLE Loans (
    LoanId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    LoanType NVARCHAR(50) NOT NULL, -- 'Personal Loan', 'Home Loan', 'Education Loan', 'Vehicle Loan'
    LoanAmount DECIMAL(18,2) NOT NULL,
    TenureMonths INT NOT NULL,
    InterestRate DECIMAL(5,2) NOT NULL,
    MonthlyEMI DECIMAL(18,2) NOT NULL,
    TotalRepayment DECIMAL(18,2) NOT NULL,
    MonthlyIncome DECIMAL(18,2) NOT NULL,
    EmploymentType NVARCHAR(50) NOT NULL,
    Purpose NVARCHAR(255) NOT NULL,
    CreditScore INT DEFAULT 720,
    Status NVARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Approved', 'Rejected', 'Active', 'Closed'
    Remarks NVARCHAR(255) NULL,
    ApprovedBy INT NULL,
    ApprovedDate DATETIME NULL,
    DisbursedDate DATETIME NULL,
    OutstandingBalance DECIMAL(18,2) DEFAULT 0.00,
    NextEMIDate DATE NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 13. Loan Payments Table
DROP TABLE IF EXISTS LoanPayments;
CREATE TABLE LoanPayments (
    PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
    LoanId INTEGER NOT NULL REFERENCES Loans(LoanId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    PaymentDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    Amount DECIMAL(18,2) NOT NULL,
    PrincipalComponent DECIMAL(18,2) NOT NULL,
    InterestComponent DECIMAL(18,2) NOT NULL,
    RemainingBalance DECIMAL(18,2) NOT NULL,
    PaymentReference NVARCHAR(50) NOT NULL
);

-- 14. Debit Cards Table
DROP TABLE IF EXISTS DebitCards;
CREATE TABLE DebitCards (
    DebitCardId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    CardNumber NVARCHAR(20) NOT NULL UNIQUE,
    CardHolderName NVARCHAR(100) NOT NULL,
    CardType NVARCHAR(20) DEFAULT 'RuPay Platinum', -- 'RuPay Platinum', 'Visa Gold', 'Mastercard World'
    ExpiryDate NVARCHAR(10) NOT NULL, -- MM/YY
    CVV NVARCHAR(10) NOT NULL,
    PINHash NVARCHAR(256) NOT NULL,
    DailyLimit DECIMAL(18,2) DEFAULT 50000.00,
    IsInternationalEnabled INTEGER DEFAULT 0,
    IsOnlineEnabled INTEGER DEFAULT 1,
    IsContactlessEnabled INTEGER DEFAULT 1,
    Status NVARCHAR(20) DEFAULT 'Active', -- 'Active', 'Deactivated', 'Blocked'
    IssuedDate DATE DEFAULT CURRENT_DATE
);

-- 15. Credit Cards Table
DROP TABLE IF EXISTS CreditCards;
CREATE TABLE CreditCards (
    CreditCardId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    CardNumber NVARCHAR(20) NOT NULL UNIQUE,
    CardHolderName NVARCHAR(100) NOT NULL,
    CardType NVARCHAR(50) DEFAULT 'BharatTrust Regalia',
    CreditLimit DECIMAL(18,2) DEFAULT 150000.00,
    AvailableLimit DECIMAL(18,2) DEFAULT 150000.00,
    ExpiryDate NVARCHAR(10) NOT NULL,
    CVV NVARCHAR(10) NOT NULL,
    PINHash NVARCHAR(256) NOT NULL,
    RewardPoints INT DEFAULT 500,
    IsInternationalEnabled INTEGER DEFAULT 0,
    Status NVARCHAR(20) DEFAULT 'Active',
    BillingCycleDay INT DEFAULT 15,
    IssuedDate DATE DEFAULT CURRENT_DATE
);

-- 16. Cheque Requests Table
DROP TABLE IF EXISTS ChequeRequests;
CREATE TABLE ChequeRequests (
    RequestId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    NumberOfLeaves INT NOT NULL, -- 20, 50, 100
    DeliveryAddress NVARCHAR(255) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Approved', 'Dispatched', 'Delivered', 'Rejected'
    Remarks NVARCHAR(255) NULL,
    ProcessedBy INT NULL,
    RequestDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ProcessedDate DATETIME NULL
);

-- 17. Service Requests Table (Profile updates, account closures, etc.)
DROP TABLE IF EXISTS ServiceRequests;
CREATE TABLE ServiceRequests (
    ServiceRequestId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    RequestType NVARCHAR(50) NOT NULL, -- 'Address Change', 'Mobile Update', 'Email Update', 'PAN Update', 'Nominee Change', 'Account Closure'
    OldValue NVARCHAR(255) NULL,
    NewValue NVARCHAR(255) NOT NULL,
    Reason NVARCHAR(255) NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Approved', 'Rejected'
    Remarks NVARCHAR(255) NULL,
    ProcessedBy INT NULL,
    RequestDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ProcessedDate DATETIME NULL
);

-- 18. Bill Payments Table
DROP TABLE IF EXISTS BillPayments;
CREATE TABLE BillPayments (
    BillPaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    BillerCategory NVARCHAR(50) NOT NULL, -- 'Electricity', 'Water', 'Gas', 'Mobile', 'DTH', 'College Fee', 'Tax'
    BillerName NVARCHAR(100) NOT NULL,
    ConsumerNumber NVARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionReference NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Success',
    PaymentDate DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 19. Investments Table
DROP TABLE IF EXISTS Investments;
CREATE TABLE Investments (
    InvestmentId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    InvestmentType NVARCHAR(50) NOT NULL, -- 'Mutual Funds', 'SIP', 'Gold Investment', 'Government Bonds', 'IPO'
    SchemeName NVARCHAR(100) NOT NULL,
    InvestedAmount DECIMAL(18,2) NOT NULL,
    CurrentValue DECIMAL(18,2) NOT NULL,
    Units DECIMAL(18,4) DEFAULT 1.0,
    NAV DECIMAL(18,2) DEFAULT 100.0,
    ReturnsPercent DECIMAL(5,2) DEFAULT 0.0,
    Status NVARCHAR(20) DEFAULT 'Active',
    InvestmentDate DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 20. Support Tickets Table
DROP TABLE IF EXISTS SupportTickets;
CREATE TABLE SupportTickets (
    TicketId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    TicketNumber NVARCHAR(20) NOT NULL UNIQUE,
    Subject NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50) NOT NULL, -- 'Account', 'Transfer', 'Cards', 'Loans', 'Other'
    Message TEXT NOT NULL,
    Priority NVARCHAR(20) DEFAULT 'Normal', -- 'Low', 'Normal', 'High'
    Status NVARCHAR(20) DEFAULT 'Open', -- 'Open', 'In Progress', 'Resolved', 'Closed'
    AssignedEmployeeId INT NULL,
    Resolution TEXT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    ResolvedAt DATETIME NULL
);

-- 21. Live Chat / Messages Table
DROP TABLE IF EXISTS ChatMessages;
CREATE TABLE ChatMessages (
    MessageId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    Sender NVARCHAR(20) NOT NULL, -- 'Customer', 'Agent', 'Bot'
    SenderName NVARCHAR(100) NOT NULL,
    Message TEXT NOT NULL,
    IsRead INTEGER DEFAULT 0,
    SentAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 22. Notifications Table
DROP TABLE IF EXISTS Notifications;
CREATE TABLE Notifications (
    NotificationId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    Title NVARCHAR(100) NOT NULL,
    Message NVARCHAR(255) NOT NULL,
    NotificationType NVARCHAR(30) DEFAULT 'General', -- 'Security', 'Transaction', 'Alert', 'Promotional'
    IsRead INTEGER DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 23. Login History & Devices Table
DROP TABLE IF EXISTS LoginHistory;
CREATE TABLE LoginHistory (
    LoginId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    IPAddress NVARCHAR(50) DEFAULT '127.0.0.1',
    Browser NVARCHAR(100) DEFAULT 'Chrome / Windows',
    DeviceType NVARCHAR(50) DEFAULT 'Desktop PC',
    IsSuccess INTEGER NOT NULL,
    FailureReason NVARCHAR(100) NULL,
    LoginTime DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 24. Devices Table
DROP TABLE IF EXISTS Devices;
CREATE TABLE Devices (
    DeviceId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    DeviceName NVARCHAR(100) NOT NULL,
    DeviceType NVARCHAR(50) NOT NULL,
    Browser NVARCHAR(100) NOT NULL,
    LastIpAddress NVARCHAR(50) NOT NULL,
    IsTrusted INTEGER DEFAULT 1,
    FirstSeen DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastActive DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 25. Security Questions Table
DROP TABLE IF EXISTS SecurityQuestions;
CREATE TABLE SecurityQuestions (
    SecurityQuestionId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
    Question NVARCHAR(255) NOT NULL,
    AnswerHash NVARCHAR(256) NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 26. Fraud Alerts Table
DROP TABLE IF EXISTS FraudAlerts;
CREATE TABLE FraudAlerts (
    AlertId INTEGER PRIMARY KEY AUTOINCREMENT,
    AccountId INTEGER NOT NULL REFERENCES Accounts(AccountId),
    TransactionId INT NULL,
    TriggerRule NVARCHAR(100) NOT NULL,
    RiskLevel NVARCHAR(20) DEFAULT 'Medium', -- 'Low', 'Medium', 'High', 'Critical'
    Description NVARCHAR(255) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'New', -- 'New', 'Under Review', 'Confirmed', 'False Positive', 'Resolved'
    ReviewedBy INT NULL,
    ReviewRemarks NVARCHAR(255) NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    ReviewedAt DATETIME NULL
);

-- 27. Audit & Activity Logs Table
DROP TABLE IF EXISTS AuditLogs;
CREATE TABLE AuditLogs (
    LogId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INT NULL,
    Username NVARCHAR(50) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    IPAddress NVARCHAR(50) DEFAULT '127.0.0.1',
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 28. Dynamic Advertisements / Banners Table
DROP TABLE IF EXISTS Advertisements;
CREATE TABLE Advertisements (
    AdId INTEGER PRIMARY KEY AUTOINCREMENT,
    Title NVARCHAR(100) NOT NULL,
    Subtitle NVARCHAR(255) NOT NULL,
    TagText NVARCHAR(50) DEFAULT 'Special Offer',
    BadgeColor NVARCHAR(30) DEFAULT 'bg-primary',
    CallToActionText NVARCHAR(50) DEFAULT 'Apply Now',
    TargetUrl NVARCHAR(255) DEFAULT '/Customer/Loans/Apply',
    ImageUrl NVARCHAR(255) DEFAULT '/images/banner-loan.jpg',
    DisplayOrder INT DEFAULT 1,
    IsActive INTEGER DEFAULT 1,
    StartDate DATE DEFAULT CURRENT_DATE,
    EndDate DATE DEFAULT (date('now', '+1 year')),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 29. Dynamic FAQs Table
DROP TABLE IF EXISTS FAQs;
CREATE TABLE FAQs (
    FAQId INTEGER PRIMARY KEY AUTOINCREMENT,
    Category NVARCHAR(50) NOT NULL, -- 'General', 'Accounts', 'Transfers', 'Loans', 'Cards', 'Security'
    Question NVARCHAR(255) NOT NULL,
    Answer TEXT NOT NULL,
    DisplayOrder INT DEFAULT 1,
    IsActive INTEGER DEFAULT 1
);

-- 30. Dynamic Offers Table
DROP TABLE IF EXISTS Offers;
CREATE TABLE Offers (
    OfferId INTEGER PRIMARY KEY AUTOINCREMENT,
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    DiscountCode NVARCHAR(50) NULL,
    ValidTill DATE NOT NULL,
    IsActive INTEGER DEFAULT 1
);

-- 31. Dynamic Currency Exchange Rates Table
DROP TABLE IF EXISTS CurrencyRates;
CREATE TABLE CurrencyRates (
    CurrencyId INTEGER PRIMARY KEY AUTOINCREMENT,
    CurrencyCode NVARCHAR(5) NOT NULL UNIQUE,
    CurrencyName NVARCHAR(50) NOT NULL,
    BuyRate DECIMAL(10,4) NOT NULL,
    SellRate DECIMAL(10,4) NOT NULL,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 32. Dynamic Interest Rates Table
DROP TABLE IF EXISTS InterestRates;
CREATE TABLE InterestRates (
    RateId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductType NVARCHAR(50) NOT NULL, -- 'Savings', 'FD', 'RD', 'Home Loan', 'Personal Loan', 'Car Loan', 'Education Loan'
    TenureOrTier NVARCHAR(50) NOT NULL, -- 'Regular', '1 Year', '2-3 Years', '5+ Years'
    RegularRate DECIMAL(5,2) NOT NULL,
    SeniorCitizenRate DECIMAL(5,2) NOT NULL,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 33. Feedback & Complaints Table
DROP TABLE IF EXISTS Feedback;
CREATE TABLE Feedback (
    FeedbackId INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customers(CustomerId),
    Rating INT NOT NULL, -- 1 to 5
    Category NVARCHAR(50) NOT NULL,
    Comments TEXT NOT NULL,
    SubmittedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);


