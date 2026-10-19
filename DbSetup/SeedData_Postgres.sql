-- =============================================
-- BharatTrust Bank - Seed Data Script
-- Populates Realistic official live Data
-- Default Password for Admin: Admin@123
-- Default Password for all others: Password@123
-- =============================================

-- 1. System Settings
DELETE FROM SystemSettings;
INSERT INTO SystemSettings (SettingKey, SettingValue, Description) VALUES
('BankName', 'BharatBank', 'Official Name of the Bank'),
('BankTagline', 'Suraksha Aur Vishwas', 'Bank Slogan'),
('BankIFSC', 'BHTT0001001', 'Default Head Office IFSC'),
('BankBranch', 'Main Branch, Nariman Point, Mumbai', 'Default Branch'),
('SupportEmail', 'support@bharatbank.com', 'Customer Support Email'),
('TollFreeNumber', '1800-200-8899', 'Customer Care Helpline'),
('LowBalanceThreshold', '5000', 'Threshold in INR for low balance alerts'),
('InactiveAccountMonths', '6', 'Months without transaction to mark account inactive'),
('MaxFailedLoginAttempts', '5', 'Max failed login attempts before lockout'),
('IssystemMode', 'true', 'Simulates OTP and External Payment Gateways'),
('ThreeDEnabled', 'true', 'Global toggle for 3D visual design effects');

-- 2. Account Types
DELETE FROM AccountTypes;
INSERT INTO AccountTypes (TypeName, InterestRate, MinimumBalance, Description) VALUES
('Savings Account', 4.00, 1000.00, 'Standard individual savings account with net banking & debit card'),
('Current Account', 0.00, 5000.00, 'Commercial business account with high daily transaction limits'),
('Salary Account', 4.25, 0.00, 'Zero-balance corporate salary account with exclusive benefits');

-- 3. Users Seed
-- Admin Hash: jkiO+JkSrbvXU4Ipn+5OXM5aKJXHsksuls02xA4zr/g=
-- Password@123 Hash: /fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=
-- Salt: BTB_SALT_2026

DELETE FROM Users;
-- Admin
INSERT INTO Users (Username, Email, PasswordHash, Salt, Role, FullName, MobileNumber, IsActive)
VALUES ('admin', 'admin@bharattrustbank.com', 'jkiO+JkSrbvXU4Ipn+5OXM5aKJXHsksuls02xA4zr/g=', 'BTB_SALT_2026', 'Admin', 'Chief Administrator', '9820011223', 1);

-- Managers
INSERT INTO Users (Username, Email, PasswordHash, Salt, Role, FullName, MobileNumber, IsActive) VALUES
('manager.sharma', 'ramesh.sharma@bharattrustbank.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Manager', 'Ramesh Sharma', '9820022334', 1),
('manager.patel', 'pooja.patel@bharattrustbank.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Manager', 'Pooja Patel', '9820033445', 1);

-- Employees
INSERT INTO Users (Username, Email, PasswordHash, Salt, Role, FullName, MobileNumber, IsActive) VALUES
('emp.vikram', 'vikram.verma@bharattrustbank.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Employee', 'Vikram Verma', '9820044556', 1),
('emp.priya', 'priya.nair@bharattrustbank.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Employee', 'Priya Nair', '9820055667', 1),
('emp.amit', 'amit.joshi@bharattrustbank.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Employee', 'Amit Joshi', '9820066778', 1),
('emp.neha', 'neha.gupta@bharattrustbank.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Employee', 'Neha Gupta', '9820077889', 1),
('emp.rohit', 'rohit.mehra@bharattrustbank.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Employee', 'Rohit Mehra', '9820088990', 1);

-- 10 Customers
INSERT INTO Users (Username, Email, PasswordHash, Salt, Role, FullName, MobileNumber, IsActive) VALUES
('rahul.sharma', 'rahul.sharma@gmail.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Rahul Sharma', '9819123456', 1),
('sneha.patel', 'sneha.patel@yahoo.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Sneha Patel', '9819234567', 1),
('ananya.deshmukh', 'ananya.d@gmail.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Ananya Deshmukh', '9819345678', 1),
('rohit.verma', 'rohit.v@outlook.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Rohit Verma', '9819456789', 1),
('priya.iyer', 'priya.iyer@gmail.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Priya Iyer', '9819567890', 1),
('arjun.singh', 'arjun.singh@gmail.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Arjun Singh', '9819678901', 1),
('kavita.joshi', 'kavita.j@gmail.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Kavita Joshi', '9819789012', 1),
('siddharth.rao', 'sid.rao@gmail.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Siddharth Rao', '9819890123', 1),
('pooja.kulkarni', 'pooja.k@gmail.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Pooja Kulkarni', '9819901234', 1),
('manish.tiwari', 'manish.t@gmail.com', '/fDP0pxZCRaKRZP5UqU2DReciPlzy1eyOA4tJkmSE1E=', 'BTB_SALT_2026', 'Customer', 'Manish Tiwari', '9819012345', 1);

-- 4. Employee Profiles
DELETE FROM Employees;
INSERT INTO Employees (UserId, EmployeeCode, Department, Designation, BranchName, DateOfJoining, Salary)
SELECT UserId, 'EMP' || LPAD(CAST(UserId AS TEXT), 4, '0'), 'Operations', 'Branch Officer', 'Main Branch - Mumbai', '2022-01-15', 55000.00
FROM Users WHERE Role IN ('Employee', 'Manager');

-- 5. Customer Profiles
DELETE FROM Customers;
INSERT INTO Customers (UserId, CustomerCode, DateOfBirth, Gender, Address, City, State, Pincode, PAN, Aadhaar, KYCStatus, NomineeName, NomineeRelation, Segment)
SELECT 
    UserId, 
    'CUST' || LPAD(CAST(UserId AS TEXT), 6, '0'),
    '1992-05-14',
    'Male',
    'Flat 402, Sea View Apartments, Bandra West',
    'Mumbai', 'Maharashtra', '400050',
    'ABCPS' || LPAD(CAST((UserId * 1111) % 10000 AS TEXT), 4, '0') || 'K',
    '2345 6789 ' || LPAD(CAST((UserId * 777) % 10000 AS TEXT), 4, '0'),
    'Verified',
    'Sunita Sharma', 'Spouse',
    CASE WHEN UserId % 3 = 0 THEN 'Premium Customer' ELSE 'Regular Customer' END
FROM Users WHERE Role = 'Customer';

-- 6. Accounts (15 accounts)
DELETE FROM Accounts;
INSERT INTO Accounts (CustomerId, AccountNumber, AccountTypeId, Balance, Currency, Status, IFSCCode, BranchName, OpenedDate)
SELECT 
    c.CustomerId,
    '100100' || LPAD(CAST(c.CustomerId AS TEXT), 6, '0'),
    1, -- Savings
    CASE WHEN c.CustomerId = 1 THEN 128500.00 
         WHEN c.CustomerId = 2 THEN 84200.00 
         WHEN c.CustomerId = 3 THEN 245000.00 
         ELSE 45000.00 + (c.CustomerId * 8500.00) END,
    'INR',
    'Active',
    'BHTT0001001',
    'Main Branch, Nariman Point, Mumbai',
    CURRENT_TIMESTAMP - INTERVAL '180 days'
FROM Customers c;

-- Add secondary Current / Salary accounts for first 5 customers
INSERT INTO Accounts (CustomerId, AccountNumber, AccountTypeId, Balance, Currency, Status, IFSCCode, BranchName, OpenedDate)
SELECT 
    c.CustomerId,
    '200200' || LPAD(CAST(c.CustomerId AS TEXT), 6, '0'),
    2, -- Current
    185000.00 + (c.CustomerId * 15000.00),
    'INR',
    'Active',
    'BHTT0001001',
    'Main Branch, Nariman Point, Mumbai',
    CURRENT_TIMESTAMP - INTERVAL '90 days'
FROM Customers c WHERE c.CustomerId <= 5;

-- 7. Beneficiaries
DELETE FROM Beneficiaries;
INSERT INTO Beneficiaries (CustomerId, BeneficiaryName, AccountNumber, BankName, IFSCCode, TransferType, UPIId, MaxLimit) VALUES
(1, 'Aarav Mehta', '100100000002', 'BharatTrust Bank', 'BHTT0001001', 'IMPS', 'aarav@okbtb', 100000.00),
(1, 'Rohan Gupta', '501002345678', 'HDFC Bank', 'HDFC0000012', 'NEFT', 'rohan@okhdfc', 50000.00),
(1, 'TechCorp Solutions', '919001238877', 'State Bank of India', 'SBIN0000300', 'RTGS', 'billing@okcorp', 500000.00),
(2, 'Rahul Sharma', '100100000001', 'BharatTrust Bank', 'BHTT0001001', 'UPI', 'rahul@okbtb', 25000.00),
(3, 'Vijay Enterprises', '302004561234', 'ICICI Bank', 'ICIC0000102', 'IMPS', 'vijay@okicici', 75000.00);

-- 8. Realistic Transactions for Customer 1 (Rahul Sharma) and others
DELETE FROM Transactions;
INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status, TransactionDate) VALUES
('TXN20260801001', 1, 'Deposit', 75000.00, 128500.00, 'Credit', 'Salary', 'Monthly Corporate Salary Credit', 'Self', 'Employer Inc', 'Online', 'Completed', CURRENT_TIMESTAMP - INTERVAL '18 days'),
('TXN20260803002', 1, 'Transfer', 15000.00, 113500.00, 'Debit', 'Transfer', 'Monthly Rent Transfer to Aarav', '100100000002', 'Aarav Mehta', 'IMPS', 'Completed', CURRENT_TIMESTAMP - INTERVAL '15 days'),
('TXN20260805003', 1, 'BillPayment', 2450.00, 111050.00, 'Debit', 'Utilities', 'Electricity Bill - Adani Electricity', 'MSEDCL-998822', 'Adani Electricity', 'Online', 'Completed', CURRENT_TIMESTAMP - INTERVAL '12 days'),
('TXN20260808004', 1, 'Recharge', 799.00, 110251.00, 'Debit', 'Utilities', 'Prepaid Mobile Recharge - 9819123456', 'JIO-Prepaid', 'Reliance Jio', 'UPI', 'Completed', CURRENT_TIMESTAMP - INTERVAL '10 days'),
('TXN20260810005', 1, 'Investment', 10000.00, 100251.00, 'Debit', 'Investment', 'Monthly SIP - Bluechip Equity Fund', 'SIP-INF846K01234', 'BharatTrust Mutual Fund', 'Online', 'Completed', CURRENT_TIMESTAMP - INTERVAL '8 days'),
('TXN20260812006', 1, 'Deposit', 35000.00, 135251.00, 'Credit', 'Transfer', 'Consulting Project Honorarium', 'Self', 'TechCorp India', 'NEFT', 'Completed', CURRENT_TIMESTAMP - INTERVAL '6 days'),
('TXN20260815007', 1, 'Withdrawal', 5000.00, 130251.00, 'Debit', 'Cash', 'ATM Cash Withdrawal - Nariman Point ATM', 'ATM-001', 'Self', 'ATM', 'Completed', CURRENT_TIMESTAMP - INTERVAL '3 days'),
('TXN20260818008', 1, 'Transfer', 1751.00, 128500.00, 'Debit', 'Shopping', 'Online Grocery Shopping', 'MERCHANT-998', 'Blinkit Retail', 'UPI', 'Completed', CURRENT_TIMESTAMP - INTERVAL '1 days');

-- 9. Fixed Deposits
DELETE FROM FixedDeposits;
INSERT INTO FixedDeposits (CustomerId, AccountId, FDNumber, PrincipalAmount, TenureMonths, InterestRate, InterestEarned, MaturityAmount, StartDate, MaturityDate, Status, AutoRenew) VALUES
(1, 1, 'FD9001001', 100000.00, 12, 7.25, 7450.00, 107450.00, CURRENT_TIMESTAMP - INTERVAL '3 months', CURRENT_TIMESTAMP + INTERVAL '9 months', 'Active', 1),
(2, 2, 'FD9001002', 50000.00, 24, 7.50, 7880.00, 57880.00, CURRENT_TIMESTAMP - INTERVAL '6 months', CURRENT_TIMESTAMP + INTERVAL '18 months', 'Active', 0),
(3, 3, 'FD9001003', 200000.00, 36, 7.75, 51680.00, 251680.00, CURRENT_TIMESTAMP - INTERVAL '1 months', CURRENT_TIMESTAMP + INTERVAL '35 months', 'Active', 1);

-- 10. Recurring Deposits
DELETE FROM RecurringDeposits;
INSERT INTO RecurringDeposits (CustomerId, AccountId, RDNumber, MonthlyInstallment, TenureMonths, InterestRate, TotalDeposited, MaturityAmount, StartDate, MaturityDate, InstallmentsPaid, Status) VALUES
(1, 1, 'RD8001001', 5000.00, 12, 7.10, 20000.00, 62350.00, CURRENT_TIMESTAMP - INTERVAL '4 months', CURRENT_TIMESTAMP + INTERVAL '8 months', 4, 'Active'),
(2, 2, 'RD8001002', 2500.00, 24, 7.30, 15000.00, 64800.00, CURRENT_TIMESTAMP - INTERVAL '6 months', CURRENT_TIMESTAMP + INTERVAL '18 months', 6, 'Active');

-- 11. Loans
DELETE FROM Loans;
INSERT INTO Loans (CustomerId, LoanType, LoanAmount, TenureMonths, InterestRate, MonthlyEMI, TotalRepayment, MonthlyIncome, EmploymentType, Purpose, CreditScore, Status, ApprovedDate, DisbursedDate, OutstandingBalance, NextEMIDate) VALUES
(1, 'Home Loan', 2500000.00, 120, 8.50, 31010.00, 3721200.00, 95000.00, 'Salaried', 'Apartment Purchase in Thane', 780, 'Active', CURRENT_TIMESTAMP - INTERVAL '5 months', CURRENT_TIMESTAMP - INTERVAL '5 months', 2420000.00, CURRENT_TIMESTAMP + INTERVAL '12 days'),
(2, 'Personal Loan', 300000.00, 24, 11.50, 14050.00, 337200.00, 65000.00, 'Salaried', 'Home Renovation', 740, 'Active', CURRENT_TIMESTAMP - INTERVAL '2 months', CURRENT_TIMESTAMP - INTERVAL '2 months', 275000.00, CURRENT_TIMESTAMP + INTERVAL '8 days'),
(3, 'Vehicle Loan', 650000.00, 36, 9.20, 20730.00, 746280.00, 80000.00, 'Self-Employed', 'New Electric SUV Purchase', 760, 'Pending', NULL, NULL, 650000.00, NULL);

-- 12. Debit Cards & Credit Cards
DELETE FROM DebitCards;
INSERT INTO DebitCards (CustomerId, AccountId, CardNumber, CardHolderName, CardType, ExpiryDate, CVV, PINHash, DailyLimit, IsInternationalEnabled, Status) VALUES
(1, 1, '4532 9801 2234 1001', 'RAHUL SHARMA', 'RuPay Platinum', '08/29', '321', '1234', 75000.00, 1, 'Active'),
(2, 2, '4532 9801 2234 1002', 'SNEHA PATEL', 'Visa Gold', '11/28', '654', '1234', 50000.00, 0, 'Active'),
(3, 3, '4532 9801 2234 1003', 'ANANYA DESHMUKH', 'Mastercard World', '04/30', '789', '1234', 100000.00, 1, 'Active');

DELETE FROM CreditCards;
INSERT INTO CreditCards (CustomerId, CardNumber, CardHolderName, CardType, CreditLimit, AvailableLimit, ExpiryDate, CVV, PINHash, RewardPoints, Status) VALUES
(1, '5241 6789 4432 9001', 'RAHUL SHARMA', 'BharatTrust Regalia', 200000.00, 168500.00, '09/29', '432', '1234', 2450, 'Active'),
(2, '5241 6789 4432 9002', 'SNEHA PATEL', 'BharatTrust Millennia', 100000.00, 89400.00, '12/28', '876', '1234', 1200, 'Active');

-- 13. Dynamic Advertisements / Banners (Manageable via Admin)
DELETE FROM Advertisements;
INSERT INTO Advertisements (Title, Subtitle, TagText, BadgeColor, CallToActionText, TargetUrl, ImageUrl, DisplayOrder, IsActive) VALUES
('Festive Home Loan Bonanza', 'Special interest rate @ 8.25% p.a. with Zero Processing Fee', 'Limited Offer', 'bg-success', 'Apply Now', '/Customer/Loans', '/images/home_loan.jpg', 1, 1),
('BharatTrust Smart Fixed Deposit', 'Earn up to 7.75% p.a. with extra 0.50% for Senior Citizens', 'High Returns', 'bg-primary', 'Invest in FD', '/Customer/Deposits', '/images/fd_banner.jpg', 2, 1),
('BharatTrust Regalia Credit Card', 'Complimentary Airport Lounge Access + 4X Reward Points on Dining', 'Premium Lifestyle', 'bg-warning text-dark', 'Explore Card', '/Customer/Cards', '/images/card_banner.jpg', 3, 1),
('Digital SIP & Mutual Funds', 'Start investing in top performing equity funds starting at INR 500/mo', 'Wealth Creation', 'bg-info', 'Start SIP', '/Customer/Investments', '/images/sip_banner.jpg', 4, 1);

-- 14. Dynamic FAQs (Manageable via Admin)
DELETE FROM FAQs;
INSERT INTO FAQs (Category, Question, Answer, DisplayOrder, IsActive) VALUES
('Accounts', 'What is the minimum balance required for a BharatTrust Savings Account?', 'The minimum balance requirement for our regular Savings Account is INR 1,000 for metro branches and INR 500 for semi-urban branches.', 1, 1),
('Transfers', 'What is the difference between IMPS, NEFT, and RTGS?', 'IMPS is an instant 24x7 fund transfer service up to INR 5 Lakhs. NEFT operates in hourly batches for any amount. RTGS is real-time gross settlement meant for high-value transfers above INR 2 Lakhs.', 2, 1),
('Security', 'How does BharatTrust Bank safeguard my net banking credentials?', 'We use military-grade SHA-256 encryption, 2-Factor Authentication (OTP system), automated session timeouts, and real-time rule-based fraud detection.', 3, 1),
('Loans', 'What documents are required to apply for a Home Loan?', 'You will need PAN Card, Aadhaar Card, last 3 months salary slips, 6 months bank statements, and property allotment documents.', 4, 1),
('Deposits', 'Can I withdraw my Fixed Deposit prematurely?', 'Yes, premature withdrawal is available online through the Net Banking portal with nominal penal interest as per bank policy.', 5, 1);

-- 15. Dynamic Offers
DELETE FROM Offers;
INSERT INTO Offers (Title, Description, Category, DiscountCode, ValidTill, IsActive) VALUES
('Swiggy 20% Instant Discount', 'Get 20% off up to INR 150 on BharatTrust Debit & Credit Cards', 'Dining', 'BTBSWIGGY20', CURRENT_TIMESTAMP + INTERVAL '3 months', 1),
('MakeMyTrip Holiday Discount', 'Flat INR 2,500 off on domestic flights & hotels booking', 'Travel', 'BTBMMTFLY', CURRENT_TIMESTAMP + INTERVAL '4 months', 1),
('Amazon Great Indian Cashback', '5% Cashback on electronics & appliances using Regalia card', 'Shopping', 'BTBAMAZON5', CURRENT_TIMESTAMP + INTERVAL '2 months', 1);

-- 16. Dynamic Currency Exchange Rates
DELETE FROM CurrencyRates;
INSERT INTO CurrencyRates (CurrencyCode, CurrencyName, BuyRate, SellRate) VALUES
('USD', 'US Dollar', 86.40, 87.10),
('EUR', 'Euro', 93.80, 94.60),
('GBP', 'British Pound', 110.25, 111.40),
('AED', 'UAE Dirham', 23.50, 23.90),
('SGD', 'Singapore Dollar', 66.80, 67.40),
('CAD', 'Canadian Dollar', 62.90, 63.50);

-- 17. Dynamic Interest Rates
DELETE FROM InterestRates;
INSERT INTO InterestRates (ProductType, TenureOrTier, RegularRate, SeniorCitizenRate) VALUES
('Savings Account', 'Daily Balance', 4.00, 4.00),
('Fixed Deposit', '1 Year to < 2 Years', 7.10, 7.60),
('Fixed Deposit', '2 Years to < 3 Years', 7.40, 7.90),
('Fixed Deposit', '3 Years to 5 Years', 7.65, 8.15),
('Recurring Deposit', '12 Months', 7.00, 7.50),
('Home Loan', 'Floating (Upto 30 Yrs)', 8.50, 8.50),
('Personal Loan', 'Fixed (1 - 5 Yrs)', 10.75, 10.75),
('Vehicle Loan', 'Fixed (1 - 7 Yrs)', 8.90, 8.90),
('Education Loan', 'Moratorium + 7 Yrs', 9.25, 9.25);

-- 18. Support Tickets & Fraud Alerts Seed
DELETE FROM SupportTickets;
INSERT INTO SupportTickets (CustomerId, TicketNumber, Subject, Category, Message, Priority, Status) VALUES
(1, 'TCK2026001', 'Query regarding virtual credit card limit', 'Cards', 'I would like to increase my virtual card daily e-commerce spending limit to INR 1,00,000.', 'Normal', 'Open'),
(2, 'TCK2026002', 'Cheque book delivery tracking', 'Account', 'Requested 50 leaves cheque book last week. Please provide courier tracking status.', 'Normal', 'Resolved');

DELETE FROM FraudAlerts;
INSERT INTO FraudAlerts (AccountId, TransactionId, TriggerRule, RiskLevel, Description, Status) VALUES
(1, NULL, 'Suspicious Login Attempt', 'Medium', 'Simulated multiple login attempts from an unknown browser IP.', 'New'),
(3, NULL, 'Unusual High Value Transfer Pattern', 'High', 'Simulated transfer request above INR 5,00,000 outside normal operating hours.', 'Under Review');

-- 19. Investments Seed
DELETE FROM Investments;
INSERT INTO Investments (CustomerId, AccountId, InvestmentType, SchemeName, InvestedAmount, CurrentValue, Units, NAV, ReturnsPercent, Status) VALUES
(1, 1, 'Mutual Funds', 'BharatTrust Bluechip Large Cap Growth', 50000.00, 58400.00, 542.39, 107.67, 16.80, 'Active'),
(1, 1, 'SIP', 'BharatTrust Emerging Opportunities Fund', 25000.00, 29100.00, 485.00, 60.00, 16.40, 'Active'),
(1, 1, 'Gold Investment', 'Sovereign Digital Gold Bond 2026-I', 30000.00, 33600.00, 5.00, 6720.00, 12.00, 'Active'),
(1, 1, 'Government Bonds', 'RBI Floating Rate Savings Bonds 2026', 40000.00, 42800.00, 40.00, 1070.00, 7.00, 'Active');

-- 20. Cheque Requests Seed
DELETE FROM ChequeRequests;
INSERT INTO ChequeRequests (CustomerId, AccountId, NumberOfLeaves, DeliveryAddress, Status, Remarks) VALUES
(1, 1, 50, 'Flat 402, Sea View Apartments, Bandra West, Mumbai 400050', 'Dispatched', 'Courier AWB: BTB998822 Mumbai Express');



