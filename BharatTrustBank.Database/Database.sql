-- =============================================
-- BharatTrust Bank - Academic Banking Simulation
-- Database Creation & Schema Script
-- Target Database: BharatTrustBankDB
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'BharatTrustBankDB')
BEGIN
    CREATE DATABASE BharatTrustBankDB;
END
GO

USE BharatTrustBankDB;
GO

-- 1. System Settings Table
IF OBJECT_ID('SystemSettings', 'U') IS NOT NULL DROP TABLE SystemSettings;
CREATE TABLE SystemSettings (
    SettingKey NVARCHAR(50) PRIMARY KEY,
    SettingValue NVARCHAR(255) NOT NULL,
    Description NVARCHAR(255) NULL,
    LastUpdated DATETIME DEFAULT GETDATE()
);
GO

-- 2. Users Table (Core Auth for all roles)
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Salt NVARCHAR(50) NOT NULL,
    Role NVARCHAR(20) NOT NULL, -- 'Customer', 'Employee', 'Manager', 'Admin'
    FullName NVARCHAR(100) NOT NULL,
    MobileNumber NVARCHAR(15) NOT NULL,
    IsActive BIT DEFAULT 1,
    IsLocked BIT DEFAULT 0,
    FailedLoginAttempts INT DEFAULT 0,
    LockoutEnd DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    LastLoginAt DATETIME NULL
);
GO

-- 3. Customers Table
IF OBJECT_ID('Customers', 'U') IS NOT NULL DROP TABLE Customers;
CREATE TABLE Customers (
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
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
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 4. Employees Table
IF OBJECT_ID('Employees', 'U') IS NOT NULL DROP TABLE Employees;
CREATE TABLE Employees (
    EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
    EmployeeCode NVARCHAR(20) NOT NULL UNIQUE,
    Department NVARCHAR(50) NOT NULL,
    Designation NVARCHAR(50) NOT NULL,
    BranchName NVARCHAR(100) DEFAULT 'Main Branch - Mumbai',
    DateOfJoining DATE NOT NULL,
    Salary DECIMAL(18,2) DEFAULT 45000.00
);
GO

-- 5. Account Types Table
IF OBJECT_ID('AccountTypes', 'U') IS NOT NULL DROP TABLE AccountTypes;
CREATE TABLE AccountTypes (
    AccountTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeName NVARCHAR(50) NOT NULL, -- 'Savings Account', 'Current Account', 'Salary Account'
    InterestRate DECIMAL(5,2) DEFAULT 4.00,
    MinimumBalance DECIMAL(18,2) DEFAULT 1000.00,
    Description NVARCHAR(255) NULL
);
GO

-- 6. Accounts Table
IF OBJECT_ID('Accounts', 'U') IS NOT NULL DROP TABLE Accounts;
CREATE TABLE Accounts (
    AccountId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    AccountNumber NVARCHAR(20) NOT NULL UNIQUE,
    AccountTypeId INT NOT NULL FOREIGN KEY REFERENCES AccountTypes(AccountTypeId),
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Currency NVARCHAR(5) DEFAULT 'INR',
    Status NVARCHAR(20) DEFAULT 'Active', -- 'Active', 'Inactive', 'Frozen', 'Closed'
    IFSCCode NVARCHAR(20) DEFAULT 'BHTT0001001',
    BranchName NVARCHAR(100) DEFAULT 'Main Branch, Nariman Point, Mumbai',
    OpenedDate DATETIME DEFAULT GETDATE(),
    LastActivityDate DATETIME DEFAULT GETDATE()
);
GO

-- 7. Beneficiaries Table
IF OBJECT_ID('Beneficiaries', 'U') IS NOT NULL DROP TABLE Beneficiaries;
CREATE TABLE Beneficiaries (
    BeneficiaryId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    BeneficiaryName NVARCHAR(100) NOT NULL,
    AccountNumber NVARCHAR(20) NOT NULL,
    BankName NVARCHAR(100) NOT NULL,
    IFSCCode NVARCHAR(20) NOT NULL,
    TransferType NVARCHAR(10) DEFAULT 'IMPS', -- 'IMPS', 'NEFT', 'RTGS', 'UPI'
    UPIId NVARCHAR(50) NULL,
    MaxLimit DECIMAL(18,2) DEFAULT 50000.00,
    IsActive BIT DEFAULT 1,
    AddedDate DATETIME DEFAULT GETDATE()
);
GO

-- 8. Transactions Table
IF OBJECT_ID('Transactions', 'U') IS NOT NULL DROP TABLE Transactions;
CREATE TABLE Transactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    TransactionReference NVARCHAR(30) NOT NULL UNIQUE,
    AccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
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
    TransactionDate DATETIME DEFAULT GETDATE()
);
GO

-- 9. Scheduled Transfers Table
IF OBJECT_ID('ScheduledTransfers', 'U') IS NOT NULL DROP TABLE ScheduledTransfers;
CREATE TABLE ScheduledTransfers (
    ScheduleId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    FromAccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
    ToAccountNumber NVARCHAR(20) NOT NULL,
    BeneficiaryName NVARCHAR(100) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransferType NVARCHAR(10) NOT NULL,
    Frequency NVARCHAR(20) NOT NULL, -- 'OneTime', 'Daily', 'Weekly', 'Monthly'
    ScheduledDate DATE NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Executed', 'Cancelled'
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 10. Fixed Deposits (FD) Table
IF OBJECT_ID('FixedDeposits', 'U') IS NOT NULL DROP TABLE FixedDeposits;
CREATE TABLE FixedDeposits (
    FDId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    AccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
    FDNumber NVARCHAR(20) NOT NULL UNIQUE,
    PrincipalAmount DECIMAL(18,2) NOT NULL,
    TenureMonths INT NOT NULL,
    InterestRate DECIMAL(5,2) NOT NULL,
    InterestEarned DECIMAL(18,2) NOT NULL,
    MaturityAmount DECIMAL(18,2) NOT NULL,
    StartDate DATE NOT NULL,
    MaturityDate DATE NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Active', -- 'Active', 'Matured', 'PrematureClosed'
    AutoRenew BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 11. Recurring Deposits (RD) Table
IF OBJECT_ID('RecurringDeposits', 'U') IS NOT NULL DROP TABLE RecurringDeposits;
CREATE TABLE RecurringDeposits (
    RDId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    AccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
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
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 12. Loans Table
IF OBJECT_ID('Loans', 'U') IS NOT NULL DROP TABLE Loans;
CREATE TABLE Loans (
    LoanId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
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
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 13. Loan Payments Table
IF OBJECT_ID('LoanPayments', 'U') IS NOT NULL DROP TABLE LoanPayments;
CREATE TABLE LoanPayments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    LoanId INT NOT NULL FOREIGN KEY REFERENCES Loans(LoanId),
    AccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
    PaymentDate DATETIME DEFAULT GETDATE(),
    Amount DECIMAL(18,2) NOT NULL,
    PrincipalComponent DECIMAL(18,2) NOT NULL,
    InterestComponent DECIMAL(18,2) NOT NULL,
    RemainingBalance DECIMAL(18,2) NOT NULL,
    PaymentReference NVARCHAR(50) NOT NULL
);
GO

-- 14. Debit Cards Table
IF OBJECT_ID('DebitCards', 'U') IS NOT NULL DROP TABLE DebitCards;
CREATE TABLE DebitCards (
    DebitCardId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    AccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
    CardNumber NVARCHAR(20) NOT NULL UNIQUE,
    CardHolderName NVARCHAR(100) NOT NULL,
    CardType NVARCHAR(20) DEFAULT 'RuPay Platinum', -- 'RuPay Platinum', 'Visa Gold', 'Mastercard World'
    ExpiryDate NVARCHAR(10) NOT NULL, -- MM/YY
    CVV NVARCHAR(10) NOT NULL,
    PINHash NVARCHAR(256) NOT NULL,
    DailyLimit DECIMAL(18,2) DEFAULT 50000.00,
    IsInternationalEnabled BIT DEFAULT 0,
    IsOnlineEnabled BIT DEFAULT 1,
    IsContactlessEnabled BIT DEFAULT 1,
    Status NVARCHAR(20) DEFAULT 'Active', -- 'Active', 'Deactivated', 'Blocked'
    IssuedDate DATE DEFAULT GETDATE()
);
GO

-- 15. Credit Cards Table
IF OBJECT_ID('CreditCards', 'U') IS NOT NULL DROP TABLE CreditCards;
CREATE TABLE CreditCards (
    CreditCardId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    CardNumber NVARCHAR(20) NOT NULL UNIQUE,
    CardHolderName NVARCHAR(100) NOT NULL,
    CardType NVARCHAR(50) DEFAULT 'BharatTrust Regalia',
    CreditLimit DECIMAL(18,2) DEFAULT 150000.00,
    AvailableLimit DECIMAL(18,2) DEFAULT 150000.00,
    ExpiryDate NVARCHAR(10) NOT NULL,
    CVV NVARCHAR(10) NOT NULL,
    PINHash NVARCHAR(256) NOT NULL,
    RewardPoints INT DEFAULT 500,
    IsInternationalEnabled BIT DEFAULT 0,
    Status NVARCHAR(20) DEFAULT 'Active',
    BillingCycleDay INT DEFAULT 15,
    IssuedDate DATE DEFAULT GETDATE()
);
GO

-- 16. Cheque Requests Table
IF OBJECT_ID('ChequeRequests', 'U') IS NOT NULL DROP TABLE ChequeRequests;
CREATE TABLE ChequeRequests (
    RequestId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    AccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
    NumberOfLeaves INT NOT NULL, -- 20, 50, 100
    DeliveryAddress NVARCHAR(255) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Approved', 'Dispatched', 'Delivered', 'Rejected'
    Remarks NVARCHAR(255) NULL,
    ProcessedBy INT NULL,
    RequestDate DATETIME DEFAULT GETDATE(),
    ProcessedDate DATETIME NULL
);
GO

-- 17. Service Requests Table (Profile updates, account closures, etc.)
IF OBJECT_ID('ServiceRequests', 'U') IS NOT NULL DROP TABLE ServiceRequests;
CREATE TABLE ServiceRequests (
    ServiceRequestId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    RequestType NVARCHAR(50) NOT NULL, -- 'Address Change', 'Mobile Update', 'Email Update', 'PAN Update', 'Nominee Change', 'Account Closure'
    OldValue NVARCHAR(255) NULL,
    NewValue NVARCHAR(255) NOT NULL,
    Reason NVARCHAR(255) NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Approved', 'Rejected'
    Remarks NVARCHAR(255) NULL,
    ProcessedBy INT NULL,
    RequestDate DATETIME DEFAULT GETDATE(),
    ProcessedDate DATETIME NULL
);
GO

-- 18. Bill Payments Table
IF OBJECT_ID('BillPayments', 'U') IS NOT NULL DROP TABLE BillPayments;
CREATE TABLE BillPayments (
    BillPaymentId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    AccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
    BillerCategory NVARCHAR(50) NOT NULL, -- 'Electricity', 'Water', 'Gas', 'Mobile', 'DTH', 'College Fee', 'Tax'
    BillerName NVARCHAR(100) NOT NULL,
    ConsumerNumber NVARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionReference NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Success',
    PaymentDate DATETIME DEFAULT GETDATE()
);
GO

-- 19. Investments Table
IF OBJECT_ID('Investments', 'U') IS NOT NULL DROP TABLE Investments;
CREATE TABLE Investments (
    InvestmentId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    AccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
    InvestmentType NVARCHAR(50) NOT NULL, -- 'Mutual Funds', 'SIP', 'Gold Investment', 'Government Bonds', 'IPO'
    SchemeName NVARCHAR(100) NOT NULL,
    InvestedAmount DECIMAL(18,2) NOT NULL,
    CurrentValue DECIMAL(18,2) NOT NULL,
    Units DECIMAL(18,4) DEFAULT 1.0,
    NAV DECIMAL(18,2) DEFAULT 100.0,
    ReturnsPercent DECIMAL(5,2) DEFAULT 0.0,
    Status NVARCHAR(20) DEFAULT 'Active',
    InvestmentDate DATETIME DEFAULT GETDATE()
);
GO

-- 20. Support Tickets Table
IF OBJECT_ID('SupportTickets', 'U') IS NOT NULL DROP TABLE SupportTickets;
CREATE TABLE SupportTickets (
    TicketId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    TicketNumber NVARCHAR(20) NOT NULL UNIQUE,
    Subject NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50) NOT NULL, -- 'Account', 'Transfer', 'Cards', 'Loans', 'Other'
    Message NVARCHAR(MAX) NOT NULL,
    Priority NVARCHAR(20) DEFAULT 'Normal', -- 'Low', 'Normal', 'High'
    Status NVARCHAR(20) DEFAULT 'Open', -- 'Open', 'In Progress', 'Resolved', 'Closed'
    AssignedEmployeeId INT NULL,
    Resolution NVARCHAR(MAX) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    ResolvedAt DATETIME NULL
);
GO

-- 21. Live Chat / Messages Table
IF OBJECT_ID('ChatMessages', 'U') IS NOT NULL DROP TABLE ChatMessages;
CREATE TABLE ChatMessages (
    MessageId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    Sender NVARCHAR(20) NOT NULL, -- 'Customer', 'Agent', 'Bot'
    SenderName NVARCHAR(100) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    IsRead BIT DEFAULT 0,
    SentAt DATETIME DEFAULT GETDATE()
);
GO

-- 22. Notifications Table
IF OBJECT_ID('Notifications', 'U') IS NOT NULL DROP TABLE Notifications;
CREATE TABLE Notifications (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
    Title NVARCHAR(100) NOT NULL,
    Message NVARCHAR(255) NOT NULL,
    NotificationType NVARCHAR(30) DEFAULT 'General', -- 'Security', 'Transaction', 'Alert', 'Promotional'
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 23. Login History & Devices Table
IF OBJECT_ID('LoginHistory', 'U') IS NOT NULL DROP TABLE LoginHistory;
CREATE TABLE LoginHistory (
    LoginId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
    IPAddress NVARCHAR(50) DEFAULT '127.0.0.1',
    Browser NVARCHAR(100) DEFAULT 'Chrome / Windows',
    DeviceType NVARCHAR(50) DEFAULT 'Desktop PC',
    IsSuccess BIT NOT NULL,
    FailureReason NVARCHAR(100) NULL,
    LoginTime DATETIME DEFAULT GETDATE()
);
GO

-- 24. Devices Table
IF OBJECT_ID('Devices', 'U') IS NOT NULL DROP TABLE Devices;
CREATE TABLE Devices (
    DeviceId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
    DeviceName NVARCHAR(100) NOT NULL,
    DeviceType NVARCHAR(50) NOT NULL,
    Browser NVARCHAR(100) NOT NULL,
    LastIpAddress NVARCHAR(50) NOT NULL,
    IsTrusted BIT DEFAULT 1,
    FirstSeen DATETIME DEFAULT GETDATE(),
    LastActive DATETIME DEFAULT GETDATE()
);
GO

-- 25. Security Questions Table
IF OBJECT_ID('SecurityQuestions', 'U') IS NOT NULL DROP TABLE SecurityQuestions;
CREATE TABLE SecurityQuestions (
    SecurityQuestionId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
    Question NVARCHAR(255) NOT NULL,
    AnswerHash NVARCHAR(256) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 26. Fraud Alerts Table
IF OBJECT_ID('FraudAlerts', 'U') IS NOT NULL DROP TABLE FraudAlerts;
CREATE TABLE FraudAlerts (
    AlertId INT IDENTITY(1,1) PRIMARY KEY,
    AccountId INT NOT NULL FOREIGN KEY REFERENCES Accounts(AccountId),
    TransactionId INT NULL,
    TriggerRule NVARCHAR(100) NOT NULL,
    RiskLevel NVARCHAR(20) DEFAULT 'Medium', -- 'Low', 'Medium', 'High', 'Critical'
    Description NVARCHAR(255) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'New', -- 'New', 'Under Review', 'Confirmed', 'False Positive', 'Resolved'
    ReviewedBy INT NULL,
    ReviewRemarks NVARCHAR(255) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    ReviewedAt DATETIME NULL
);
GO

-- 27. Audit & Activity Logs Table
IF OBJECT_ID('AuditLogs', 'U') IS NOT NULL DROP TABLE AuditLogs;
CREATE TABLE AuditLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Username NVARCHAR(50) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    IPAddress NVARCHAR(50) DEFAULT '127.0.0.1',
    Timestamp DATETIME DEFAULT GETDATE()
);
GO

-- 28. Dynamic Advertisements / Banners Table
IF OBJECT_ID('Advertisements', 'U') IS NOT NULL DROP TABLE Advertisements;
CREATE TABLE Advertisements (
    AdId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL,
    Subtitle NVARCHAR(255) NOT NULL,
    TagText NVARCHAR(50) DEFAULT 'Special Offer',
    BadgeColor NVARCHAR(30) DEFAULT 'bg-primary',
    CallToActionText NVARCHAR(50) DEFAULT 'Apply Now',
    TargetUrl NVARCHAR(255) DEFAULT '/Customer/Loans/Apply',
    ImageUrl NVARCHAR(255) DEFAULT '/images/banner-loan.jpg',
    DisplayOrder INT DEFAULT 1,
    IsActive BIT DEFAULT 1,
    StartDate DATE DEFAULT GETDATE(),
    EndDate DATE DEFAULT DATEADD(year, 1, GETDATE()),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 29. Dynamic FAQs Table
IF OBJECT_ID('FAQs', 'U') IS NOT NULL DROP TABLE FAQs;
CREATE TABLE FAQs (
    FAQId INT IDENTITY(1,1) PRIMARY KEY,
    Category NVARCHAR(50) NOT NULL, -- 'General', 'Accounts', 'Transfers', 'Loans', 'Cards', 'Security'
    Question NVARCHAR(255) NOT NULL,
    Answer NVARCHAR(MAX) NOT NULL,
    DisplayOrder INT DEFAULT 1,
    IsActive BIT DEFAULT 1
);
GO

-- 30. Dynamic Offers Table
IF OBJECT_ID('Offers', 'U') IS NOT NULL DROP TABLE Offers;
CREATE TABLE Offers (
    OfferId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    DiscountCode NVARCHAR(50) NULL,
    ValidTill DATE NOT NULL,
    IsActive BIT DEFAULT 1
);
GO

-- 31. Dynamic Currency Exchange Rates Table
IF OBJECT_ID('CurrencyRates', 'U') IS NOT NULL DROP TABLE CurrencyRates;
CREATE TABLE CurrencyRates (
    CurrencyId INT IDENTITY(1,1) PRIMARY KEY,
    CurrencyCode NVARCHAR(5) NOT NULL UNIQUE,
    CurrencyName NVARCHAR(50) NOT NULL,
    BuyRate DECIMAL(10,4) NOT NULL,
    SellRate DECIMAL(10,4) NOT NULL,
    LastUpdated DATETIME DEFAULT GETDATE()
);
GO

-- 32. Dynamic Interest Rates Table
IF OBJECT_ID('InterestRates', 'U') IS NOT NULL DROP TABLE InterestRates;
CREATE TABLE InterestRates (
    RateId INT IDENTITY(1,1) PRIMARY KEY,
    ProductType NVARCHAR(50) NOT NULL, -- 'Savings', 'FD', 'RD', 'Home Loan', 'Personal Loan', 'Car Loan', 'Education Loan'
    TenureOrTier NVARCHAR(50) NOT NULL, -- 'Regular', '1 Year', '2-3 Years', '5+ Years'
    RegularRate DECIMAL(5,2) NOT NULL,
    SeniorCitizenRate DECIMAL(5,2) NOT NULL,
    LastUpdated DATETIME DEFAULT GETDATE()
);
GO

-- 33. Feedback & Complaints Table
IF OBJECT_ID('Feedback', 'U') IS NOT NULL DROP TABLE Feedback;
CREATE TABLE Feedback (
    FeedbackId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    Rating INT NOT NULL, -- 1 to 5
    Category NVARCHAR(50) NOT NULL,
    Comments NVARCHAR(MAX) NOT NULL,
    SubmittedAt DATETIME DEFAULT GETDATE()
);
GO
