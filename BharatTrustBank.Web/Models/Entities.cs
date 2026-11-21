using System;

namespace BharatTrustBank.Web.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // 'Customer', 'Employee', 'Manager', 'Admin'
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsLocked { get; set; } = false;
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class Customer
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string PAN { get; set; } = string.Empty;
        public string Aadhaar { get; set; } = string.Empty;
        public string KYCStatus { get; set; } = "Pending";
        public int? KYCVerifiedBy { get; set; }
        public DateTime? KYCVerifiedDate { get; set; }
        public string? KYCRemarks { get; set; }
        public string? NomineeName { get; set; }
        public string? NomineeRelation { get; set; }
        public string Segment { get; set; } = "Regular Customer";
        public DateTime CreatedAt { get; set; }

        // Joined fields
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public int UserId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime DateOfJoining { get; set; }
        public decimal Salary { get; set; }

        // Joined fields
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class Account
    {
        public int AccountId { get; set; }
        public int CustomerId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public int AccountTypeId { get; set; }
        public string AccountTypeName { get; set; } = "Savings Account";
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = "Active"; // Active, Inactive, Frozen, Closed
        public string IFSCCode { get; set; } = "BHTT0001001";
        public string BranchName { get; set; } = "Main Branch, Nariman Point, Mumbai";
        public DateTime OpenedDate { get; set; }
        public DateTime LastActivityDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }

    public class Transaction
    {
        public int TransactionId { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty; // Deposit, Withdrawal, Transfer, BillPayment, Recharge, Investment, LoanEMI
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string DebitOrCredit { get; set; } = "Debit"; // Debit, Credit
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? DestinationAccount { get; set; }
        public string? BeneficiaryName { get; set; }
        public string PaymentMethod { get; set; } = "Online";
        public string Status { get; set; } = "Completed";
        public DateTime TransactionDate { get; set; }
    }

    public class Beneficiary
    {
        public int BeneficiaryId { get; set; }
        public int CustomerId { get; set; }
        public string BeneficiaryName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string IFSCCode { get; set; } = string.Empty;
        public string TransferType { get; set; } = "IMPS";
        public string? UPIId { get; set; }
        public decimal MaxLimit { get; set; } = 50000;
        public bool IsActive { get; set; } = true;
        public DateTime AddedDate { get; set; }
    }

    public class FixedDeposit
    {
        public int FDId { get; set; }
        public int CustomerId { get; set; }
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string FDNumber { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public int TenureMonths { get; set; }
        public decimal InterestRate { get; set; }
        public decimal InterestEarned { get; set; }
        public decimal MaturityAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime MaturityDate { get; set; }
        public string Status { get; set; } = "Active";
        public bool AutoRenew { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RecurringDeposit
    {
        public int RDId { get; set; }
        public int CustomerId { get; set; }
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string RDNumber { get; set; } = string.Empty;
        public decimal MonthlyInstallment { get; set; }
        public int TenureMonths { get; set; }
        public decimal InterestRate { get; set; }
        public decimal TotalDeposited { get; set; }
        public decimal MaturityAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime MaturityDate { get; set; }
        public int InstallmentsPaid { get; set; } = 1;
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
    }

    public class Loan
    {
        public int LoanId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string LoanType { get; set; } = string.Empty;
        public decimal LoanAmount { get; set; }
        public int TenureMonths { get; set; }
        public decimal InterestRate { get; set; }
        public decimal MonthlyEMI { get; set; }
        public decimal TotalRepayment { get; set; }
        public decimal MonthlyIncome { get; set; }
        public string EmploymentType { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public int CreditScore { get; set; } = 720;
        public string Status { get; set; } = "Pending";
        public string? Remarks { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? DisbursedDate { get; set; }
        public decimal OutstandingBalance { get; set; }
        public DateTime? NextEMIDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DebitCard
    {
        public int DebitCardId { get; set; }
        public int CustomerId { get; set; }
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string CardType { get; set; } = "RuPay Platinum";
        public string ExpiryDate { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public string PINHash { get; set; } = string.Empty;
        public decimal DailyLimit { get; set; } = 50000;
        public bool IsInternationalEnabled { get; set; }
        public bool IsOnlineEnabled { get; set; } = true;
        public bool IsContactlessEnabled { get; set; } = true;
        public string Status { get; set; } = "Active";
        public DateTime IssuedDate { get; set; }
    }

    public class CreditCard
    {
        public int CreditCardId { get; set; }
        public int CustomerId { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string CardType { get; set; } = "BharatTrust Regalia";
        public decimal CreditLimit { get; set; } = 150000;
        public decimal AvailableLimit { get; set; } = 150000;
        public string ExpiryDate { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public string PINHash { get; set; } = string.Empty;
        public int RewardPoints { get; set; } = 500;
        public bool IsInternationalEnabled { get; set; }
        public string Status { get; set; } = "Active";
        public int BillingCycleDay { get; set; } = 15;
        public DateTime IssuedDate { get; set; }
    }

    public class ChequeRequest
    {
        public int RequestId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public int NumberOfLeaves { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? Remarks { get; set; }
        public int? ProcessedBy { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
    }

    public class ServiceRequest
    {
        public int ServiceRequestId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string NewValue { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Remarks { get; set; }
        public int? ProcessedBy { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
    }

    public class BillPayment
    {
        public int BillPaymentId { get; set; }
        public int CustomerId { get; set; }
        public int AccountId { get; set; }
        public string BillerCategory { get; set; } = string.Empty;
        public string BillerName { get; set; } = string.Empty;
        public string ConsumerNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public string Status { get; set; } = "Success";
        public DateTime PaymentDate { get; set; }
    }

    public class Investment
    {
        public int InvestmentId { get; set; }
        public int CustomerId { get; set; }
        public int AccountId { get; set; }
        public string InvestmentType { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public decimal InvestedAmount { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal Units { get; set; }
        public decimal NAV { get; set; }
        public decimal ReturnsPercent { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime InvestmentDate { get; set; }
    }

    public class SupportTicket
    {
        public int TicketId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public string Status { get; set; } = "Open";
        public int? AssignedEmployeeId { get; set; }
        public string? Resolution { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class ChatMessage
    {
        public int MessageId { get; set; }
        public int CustomerId { get; set; }
        public string Sender { get; set; } = "Customer"; // Customer, Agent, Bot
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = "General";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FraudAlert
    {
        public int AlertId { get; set; }
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int? TransactionId { get; set; }
        public string TriggerRule { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High, Critical
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "New"; // New, Under Review, Confirmed, False Positive, Resolved
        public int? ReviewedBy { get; set; }
        public string? ReviewRemarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class AuditLog
    {
        public int LogId { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IPAddress { get; set; } = "127.0.0.1";
        public DateTime Timestamp { get; set; }
    }

    public class Advertisement
    {
        public int AdId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string TagText { get; set; } = "Special Offer";
        public string BadgeColor { get; set; } = "bg-primary";
        public string CallToActionText { get; set; } = "Apply Now";
        public string TargetUrl { get; set; } = "/Customer/Loans";
        public string ImageUrl { get; set; } = "/images/banner-loan.jpg";
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class FAQ
    {
        public int FAQId { get; set; }
        public string Category { get; set; } = "General";
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }

    public class Offer
    {
        public int OfferId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? DiscountCode { get; set; }
        public DateTime ValidTill { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CurrencyRate
    {
        public int CurrencyId { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class InterestRate
    {
        public int RateId { get; set; }
        public string ProductType { get; set; } = string.Empty;
        public string TenureOrTier { get; set; } = string.Empty;
        public decimal RegularRate { get; set; }
        public decimal SeniorCitizenRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
