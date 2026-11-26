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
    public class AccountsController : Controller
    {
        private readonly DatabaseHelper _db;

        public AccountsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            return View(accounts);
        }

        public IActionResult Details(int id)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var account = _db.GetAccountById(id);
            if (account == null || account.CustomerId != customerId)
            {
                TempData["ErrorMessage"] = "Account not found or access unauthorized.";
                return RedirectToAction("Index");
            }

            var transactions = _db.GetTransactionsByCustomerId(customerId, 50);
            ViewBag.Transactions = transactions.FindAll(t => t.AccountId == id);
            return View(account);
        }

        public IActionResult Passbook(int id)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var account = _db.GetAccountById(id);
            if (account == null || account.CustomerId != customerId)
            {
                TempData["ErrorMessage"] = "Account not found.";
                return RedirectToAction("Index");
            }

            var transactions = _db.GetTransactionsByCustomerId(customerId, 100);
            ViewBag.Transactions = transactions.FindAll(t => t.AccountId == id);
            return View(account);
        }

        [HttpGet]
        public IActionResult Deposit()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            var model = new DepositWithdrawViewModel
            {
                TransactionType = "Deposit",
                Description = "Cash Deposit system",
                AvailableAccounts = accounts
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deposit(DepositWithdrawViewModel model)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (!ModelState.IsValid)
            {
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                return View(model);
            }

            var result = _db.ExecuteDeposit(customerId, model.AccountId, model.Amount, model.Description.Trim());
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                return View(model);
            }

            TempData["SuccessMessage"] = $"INR {model.Amount:N2} deposited successfully! Ref: {result.Reference}";
            return RedirectToAction("Details", new { id = model.AccountId });
        }

        [HttpGet]
        public IActionResult Withdraw()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            var model = new DepositWithdrawViewModel
            {
                TransactionType = "Withdrawal",
                Description = "ATM Cash Withdrawal system",
                AvailableAccounts = accounts
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Withdraw(DepositWithdrawViewModel model)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (!ModelState.IsValid)
            {
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                return View(model);
            }

            var result = _db.ExecuteWithdrawal(customerId, model.AccountId, model.Amount, model.Description.Trim());
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                return View(model);
            }

            TempData["SuccessMessage"] = $"INR {model.Amount:N2} withdrawn successfully! Ref: {result.Reference}";
            return RedirectToAction("Details", new { id = model.AccountId });
        }
    }
}

