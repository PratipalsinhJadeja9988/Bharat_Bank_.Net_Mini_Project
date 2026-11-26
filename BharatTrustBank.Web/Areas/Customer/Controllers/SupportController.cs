using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Helpers;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [RoleAuthorize("Customer")]
    public class SupportController : Controller
    {
        private readonly DatabaseHelper _db;

        public SupportController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var tickets = _db.GetSupportTicketsByCustomerId(customerId);
            var messages = _db.GetChatMessages(customerId);

            ViewBag.Tickets = tickets;
            ViewBag.ChatMessages = messages;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTicket(string subject, string category, string message, string priority)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Subject and message are required.";
                return RedirectToAction("Index");
            }

            string ticketNum = _db.CreateSupportTicket(customerId, subject, category, message, priority);
            TempData["SuccessMessage"] = $"Support ticket {ticketNum} created successfully. An executive will respond shortly.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendChatMessage(string userMessage)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            string customerName = SessionHelper.GetFullName(HttpContext.Session) ?? "Customer";

            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return RedirectToAction("Index");
            }

            // Save Customer Message
            _db.SaveChatMessage(customerId, "Customer", customerName, userMessage);

            // Automated Banking Chatbot response logic (Rule-based tool and knowledge base)
            string botResponse = GenerateChatbotResponse(customerId, userMessage);
            _db.SaveChatMessage(customerId, "Bot", "BharatBank Assistant", botResponse);

            return RedirectToAction("Index");
        }

        private string GenerateChatbotResponse(int customerId, string query)
        {
            string q = query.ToLower();

            // Dynamic Tool: GetOwnBalance
            if (q.Contains("balance") || q.Contains("how much") || q.Contains("my money"))
            {
                var accounts = _db.GetAccountsByCustomerId(customerId);
                if (accounts.Any())
                {
                    decimal total = accounts.Sum(a => a.Balance);
                    string details = string.Join(", ", accounts.Select(a => $"{a.AccountTypeName} ({a.AccountNumber}): ₹{a.Balance:N2}"));
                    return $"Your total available balance is ₹{total:N2} across {accounts.Count} account(s). Details: {details}.";
                }
                return "You do not have any active bank accounts registered at this moment.";
            }

            // Dynamic Tool: GetOwnTransactions
            if (q.Contains("recent transaction") || q.Contains("statement") || q.Contains("last transaction") || q.Contains("history"))
            {
                var txns = _db.GetTransactionsByCustomerId(customerId, 3);
                if (txns.Any())
                {
                    string list = string.Join("; ", txns.Select(t => $"{t.TransactionDate:dd MMM} - {t.Description}: {(t.DebitOrCredit == "Credit" ? "+" : "-")}₹{t.Amount:N2}"));
                    return $"Here are your 3 most recent transactions: {list}. You can view complete passbooks in the Accounts tab.";
                }
                return "No recent transactions found on your accounts.";
            }

            // Dynamic Tool: GetOwnLoanStatus
            if (q.Contains("my loan") || q.Contains("loan status") || q.Contains("emi status"))
            {
                var loans = _db.GetLoansByCustomerId(customerId);
                if (loans.Any())
                {
                    string list = string.Join(", ", loans.Select(l => $"{l.LoanType}: ₹{l.LoanAmount:N0} (EMI: ₹{l.MonthlyEMI:N0}/mo, {l.Status})"));
                    return $"Your active loan portfolio: {list}. Next EMI is due on the 5th of next month.";
                }
                return "You have no active loans currently. You can calculate eligibility and apply under the Loans tab.";
            }

            // Static / FAQ Responses
            if (q.Contains("fd") || q.Contains("fixed deposit"))
                return "Fixed Deposits offer guaranteed returns up to 8.15% p.a. with minimum investment of ₹5,000. You can open an instant FD from the 'Deposits' tab.";
            if (q.Contains("rd") || q.Contains("recurring deposit"))
                return "Recurring Deposits start at ₹1,000/month with flexible tenures from 6 months to 5 years offering up to 7.85% p.a.";
            if (q.Contains("transfer") || q.Contains("imps") || q.Contains("neft") || q.Contains("upi"))
                return "You can transfer money instantly via IMPS (up to ₹5 Lakhs 24x7) or UPI from the 'Transfers' menu. Ensure you have added the beneficiary first.";
            if (q.Contains("card") || q.Contains("block") || q.Contains("pin"))
                return "You can manage your debit/credit cards, reset 4-digit ATM PIN, adjust limits, or temporarily freeze your card under the 'Cards' menu.";
            if (q.Contains("loan") || q.Contains("home loan") || q.Contains("personal loan"))
                return "We offer Home Loans (8.50%), Vehicle Loans (8.90%), and Personal Loans (10.75%). You can check your eligibility from the 'Analytics' or 'Loans' menu.";
            if (q.Contains("cheque") || q.Contains("passbook"))
                return "To request a new 20/50/100 leaves cheque book or download PDF statements, visit the 'Services & Requests' or 'Accounts' section.";
            
            return "Namaste! I am BharatBank Assistant. You can ask me about your available balance, recent transactions, active loans, FD/RD interest rates, or transfer options.";
        }
    }
}
