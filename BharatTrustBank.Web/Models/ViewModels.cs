using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BharatTrustBank.Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username or Email is required")]
        [Display(Name = "Username / Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your role")]
        [Display(Name = "Login As Role")]
        public string Role { get; set; } = "Customer"; // Customer, Employee, Manager, Admin

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name (as per PAN/Aadhaar)")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Username must be between 4 and 20 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "10-digit mobile number is required")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian Mobile Number")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-25);

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; } = "Male";

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = "Mumbai";

        [Required(ErrorMessage = "State is required")]
        public string State { get; set; } = "Maharashtra";

        [Required(ErrorMessage = "6-digit PIN code is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter a valid 6-digit PIN Code")]
        public string Pincode { get; set; } = string.Empty;

        [Required(ErrorMessage = "10-digit PAN number is required")]
        [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Enter a valid PAN format (e.g. ABCDE1234F)")]
        [Display(Name = "PAN Number")]
        public string PAN { get; set; } = string.Empty;

        [Required(ErrorMessage = "12-digit Aadhaar number is required")]
        [RegularExpression(@"^\d{4}\s?\d{4}\s?\d{4}$", ErrorMessage = "Enter a valid 12-digit Aadhaar Number")]
        [Display(Name = "Aadhaar Number")]
        public string Aadhaar { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account Type is required")]
        [Display(Name = "Initial Account Type")]
        public int AccountTypeId { get; set; } = 1;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept Terms and Conditions to register")]
        public bool AcceptTerms { get; set; }

        public string? NomineeName { get; set; }
        public string? NomineeRelation { get; set; }
    }

    public class CustomerDashboardViewModel
    {
        public Customer Customer { get; set; } = new();
        public List<Account> Accounts { get; set; } = new();
        public decimal TotalBalance { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalLoansOutstanding { get; set; }
        public List<Transaction> RecentTransactions { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public List<Advertisement> Banners { get; set; } = new();
        public List<DebitCard> DebitCards { get; set; } = new();
        public List<CreditCard> CreditCards { get; set; } = new();
        public SpendingInsightsViewModel SpendingInsights { get; set; } = new();
        public int UnreadNotificationCount { get; set; }
    }

    public class TransferViewModel
    {
        [Required(ErrorMessage = "Please select source account")]
        [Display(Name = "From Account")]
        public int FromAccountId { get; set; }

        [Required(ErrorMessage = "Please select transfer method")]
        [Display(Name = "Transfer Mode")]
        public string TransferType { get; set; } = "IMPS"; // IMPS, NEFT, RTGS, UPI

        [Required(ErrorMessage = "Beneficiary Name is required")]
        [Display(Name = "Beneficiary Name")]
        public string BeneficiaryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account Number / UPI ID is required")]
        [Display(Name = "Destination Account / UPI")]
        public string DestinationAccount { get; set; } = string.Empty;

        [Display(Name = "Bank IFSC Code")]
        public string IFSCCode { get; set; } = "BHTT0001001";

        [Required(ErrorMessage = "Transfer amount is required")]
        [Range(1, 1000000, ErrorMessage = "Amount must be between INR 1 and INR 10,00,000")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Remarks or description is required")]
        [Display(Name = "Remarks / Purpose")]
        public string Description { get; set; } = "Fund Transfer";

        [Display(Name = "Simulated 6-Digit OTP")]
        public string? OTP { get; set; }

        public List<Account> AvailableAccounts { get; set; } = new();
        public List<Beneficiary> SavedBeneficiaries { get; set; } = new();
    }

    public class DepositWithdrawViewModel
    {
        [Required(ErrorMessage = "Please select an account")]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(100, 500000, ErrorMessage = "Amount must be between INR 100 and INR 5,00,000")]
        public decimal Amount { get; set; }

        [Required]
        public string TransactionType { get; set; } = "Deposit"; // Deposit or Withdrawal

        [Required]
        public string Description { get; set; } = "Cash Deposit system";

        public List<Account> AvailableAccounts { get; set; } = new();
    }

    public class FDViewModel
    {
        [Required(ErrorMessage = "Please select debit account")]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Deposit amount is required")]
        [Range(5000, 10000000, ErrorMessage = "Minimum FD amount is INR 5,000")]
        [Display(Name = "Principal Amount (INR)")]
        public decimal PrincipalAmount { get; set; } = 50000;

        [Required]
        [Range(6, 120, ErrorMessage = "Tenure must be between 6 and 120 months")]
        [Display(Name = "Tenure (in Months)")]
        public int TenureMonths { get; set; } = 12;

        public decimal InterestRate { get; set; } = 7.10m;
        public decimal EstimatedInterest { get; set; }
        public decimal EstimatedMaturityAmount { get; set; }
        public bool AutoRenew { get; set; } = true;
        public bool IsSeniorCitizen { get; set; }

        public List<Account> AvailableAccounts { get; set; } = new();
        public List<FixedDeposit> ExistingFDs { get; set; } = new();
    }

    public class RDViewModel
    {
        [Required(ErrorMessage = "Please select debit account")]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Monthly installment is required")]
        [Range(500, 100000, ErrorMessage = "Monthly installment must be between INR 500 and INR 1,00,000")]
        [Display(Name = "Monthly Installment (INR)")]
        public decimal MonthlyInstallment { get; set; } = 2500;

        [Required]
        [Range(6, 60, ErrorMessage = "Tenure must be between 6 and 60 months")]
        [Display(Name = "Tenure (in Months)")]
        public int TenureMonths { get; set; } = 12;

        public decimal InterestRate { get; set; } = 7.00m;
        public decimal TotalInvestment { get; set; }
        public decimal EstimatedMaturityAmount { get; set; }

        public List<Account> AvailableAccounts { get; set; } = new();
        public List<RecurringDeposit> ExistingRDs { get; set; } = new();
    }

    public class LoanApplyViewModel
    {
        [Required(ErrorMessage = "Loan type is required")]
        [Display(Name = "Loan Category")]
        public string LoanType { get; set; } = "Personal Loan"; // Personal Loan, Home Loan, Vehicle Loan, Education Loan

        [Required(ErrorMessage = "Loan amount is required")]
        [Range(10000, 50000000, ErrorMessage = "Loan amount must be valid")]
        [Display(Name = "Required Loan Amount (INR)")]
        public decimal LoanAmount { get; set; } = 200000;

        [Required(ErrorMessage = "Tenure in months is required")]
        [Range(6, 360, ErrorMessage = "Tenure must be between 6 and 360 months")]
        [Display(Name = "Tenure (Months)")]
        public int TenureMonths { get; set; } = 24;

        [Required(ErrorMessage = "Monthly income is required")]
        [Range(15000, 5000000, ErrorMessage = "Minimum monthly income is INR 15,000")]
        [Display(Name = "Monthly Net Income (INR)")]
        public decimal MonthlyIncome { get; set; } = 60000;

        [Required(ErrorMessage = "Employment type is required")]
        public string EmploymentType { get; set; } = "Salaried"; // Salaried, Self-Employed, Business

        [Required(ErrorMessage = "Purpose is required")]
        public string Purpose { get; set; } = "Home Improvement";

        public decimal InterestRate { get; set; } = 10.75m;
        public decimal EstimatedEMI { get; set; }
        public decimal TotalRepayment { get; set; }
        public int SimulatedCreditScore { get; set; } = 740;

        public List<Loan> ExistingLoans { get; set; } = new();
    }

    public class BillPayViewModel
    {
        [Required]
        public int AccountId { get; set; }

        [Required]
        [Display(Name = "Biller Category")]
        public string BillerCategory { get; set; } = "Electricity"; // Electricity, Water, Gas, Mobile, DTH, College Fee, Tax

        [Required(ErrorMessage = "Biller Provider is required")]
        [Display(Name = "Service Provider")]
        public string BillerName { get; set; } = "Adani Electricity Mumbai";

        [Required(ErrorMessage = "Consumer/Account Number is required")]
        [Display(Name = "Consumer ID / Service No")]
        public string ConsumerNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bill Amount is required")]
        [Range(10, 500000, ErrorMessage = "Amount must be between INR 10 and INR 5,00,000")]
        public decimal Amount { get; set; }

        public List<Account> AvailableAccounts { get; set; } = new();
        public List<BillPayment> PaymentHistory { get; set; } = new();
    }

    public class SpendingInsightsViewModel
    {
        public decimal MonthlyTotalSpending { get; set; }
        public decimal MonthlyTotalIncome { get; set; }
        public decimal SavingsRate { get; set; }
        public string TopSpendingCategory { get; set; } = "Utilities";
        public decimal TopCategoryAmount { get; set; }
        public string FinancialTip { get; set; } = "Great job! Your savings rate is above 25% this month.";
        public Dictionary<string, decimal> CategoryBreakdown { get; set; } = new();
        public List<MonthlySpendTrend> MonthlyTrends { get; set; } = new();
    }

    public class MonthlySpendTrend
    {
        public string Month { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }

    public class CreditScoreViewModel
    {
        [Required]
        [Display(Name = "Monthly Income (INR)")]
        public decimal MonthlyIncome { get; set; } = 75000;

        [Required]
        [Display(Name = "Existing Total EMIs (INR)")]
        public decimal ExistingEMIs { get; set; } = 15000;

        [Required]
        [Display(Name = "Credit Card Limit (INR)")]
        public decimal CreditCardLimit { get; set; } = 100000;

        [Required]
        [Display(Name = "Average Monthly Card Usage (INR)")]
        public decimal MonthlyCardUsage { get; set; } = 25000;

        [Required]
        [Display(Name = "Years of Banking History")]
        public int BankingHistoryYears { get; set; } = 4;

        [Display(Name = "Any Past EMI Default?")]
        public bool HasPastDefault { get; set; } = false;

        public int CalculatedScore { get; set; }
        public string ScoreCategory { get; set; } = "Excellent";
        public string AssessmentReason { get; set; } = string.Empty;
    }

    public class LoanEligibilityViewModel
    {
        [Required]
        public string LoanType { get; set; } = "Home Loan";

        [Required]
        public decimal MonthlyIncome { get; set; } = 80000;

        [Required]
        public decimal ExistingEMIs { get; set; } = 10000;

        [Required]
        public decimal RequestedAmount { get; set; } = 3000000;

        [Required]
        public int TenureYears { get; set; } = 20;

        [Required]
        public int CreditScore { get; set; } = 750;

        public bool IsEligible { get; set; }
        public string EligibilityStatus { get; set; } = "Eligible";
        public decimal MaxEligibleAmount { get; set; }
        public decimal EstimatedEMI { get; set; }
        public string AssessmentSummary { get; set; } = string.Empty;
    }

    public class AdminDashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalAccounts { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalDepositsVolume { get; set; }
        public decimal TotalLoansVolume { get; set; }
        public int ActiveFraudAlerts { get; set; }
        public int PendingKYCs { get; set; }
        public int PendingServiceRequests { get; set; }
        public List<AuditLog> RecentAuditLogs { get; set; } = new();
        public List<FraudAlert> RecentFraudAlerts { get; set; } = new();
        public List<Transaction> RecentTransactions { get; set; } = new();
    }

    public class ManagerDashboardViewModel
    {
        public int TotalBranchCustomers { get; set; }
        public int TotalBranchAccounts { get; set; }
        public decimal TotalBranchDeposits { get; set; }
        public decimal TotalBranchLoans { get; set; }
        public int PendingLoanApprovals { get; set; }
        public int InactiveAccountsCount { get; set; }
        public int OpenFraudAlerts { get; set; }
        public List<Loan> PendingLoans { get; set; } = new();
        public List<Account> InactiveAccounts { get; set; } = new();
        public List<FraudAlert> BranchFraudAlerts { get; set; } = new();
    }

    public class EmployeeDashboardViewModel
    {
        public int PendingKYCVerificationCount { get; set; }
        public int PendingChequeRequestsCount { get; set; }
        public int PendingServiceRequestsCount { get; set; }
        public int OpenCustomerTicketsCount { get; set; }
        public List<Customer> PendingKYCCustomers { get; set; } = new();
        public List<ChequeRequest> PendingChequeRequests { get; set; } = new();
        public List<ServiceRequest> PendingServiceRequests { get; set; } = new();
        public List<SupportTicket> AssignedTickets { get; set; } = new();
    }

    public class ReportFilterViewModel
    {
        public string ReportType { get; set; } = "Transactions"; // Transactions, Customers, Accounts, Loans, Deposits, Fraud
        public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
        public DateTime? ToDate { get; set; } = DateTime.Today;
        public string? Status { get; set; }
        public string? Category { get; set; }
        public int? AccountId { get; set; }
        public List<dynamic> ReportResults { get; set; } = new();
    }
}

