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
    public class TransfersController : Controller
    {
        private readonly DatabaseHelper _db;

        public TransfersController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            var beneficiaries = _db.GetBeneficiariesByCustomerId(customerId);

            var model = new TransferViewModel
            {
                AvailableAccounts = accounts,
                SavedBeneficiaries = beneficiaries
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(TransferViewModel model)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (!ModelState.IsValid)
            {
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                model.SavedBeneficiaries = _db.GetBeneficiariesByCustomerId(customerId);
                return View(model);
            }

            // Simulated OTP Check
            if (string.IsNullOrEmpty(model.OTP) || model.OTP.Length != 6)
            {
                ModelState.AddModelError("OTP", "Please enter a valid 6-digit OTP (e.g. 123456)");
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                model.SavedBeneficiaries = _db.GetBeneficiariesByCustomerId(customerId);
                return View(model);
            }

            var result = _db.ExecuteFundTransfer(
                customerId, model.FromAccountId,
                model.DestinationAccount.Trim(),
                model.BeneficiaryName.Trim(),
                model.Amount,
                model.TransferType,
                model.Description.Trim()
            );

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                model.SavedBeneficiaries = _db.GetBeneficiariesByCustomerId(customerId);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Transfer of INR {model.Amount:N2} successful! Reference: {result.Reference}";
            return RedirectToAction("Receipt", new { reference = result.Reference });
        }

        public IActionResult Receipt(string reference)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var transactions = _db.GetTransactionsByCustomerId(customerId, 100);
            var txn = transactions.Find(t => t.TransactionReference == reference);
            if (txn == null)
            {
                TempData["ErrorMessage"] = "Receipt not found.";
                return RedirectToAction("Index");
            }

            return View(txn);
        }

        [HttpGet]
        public IActionResult Beneficiaries()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var list = _db.GetBeneficiariesByCustomerId(customerId);
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBeneficiary(Beneficiary model)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            model.CustomerId = customerId;

            if (string.IsNullOrWhiteSpace(model.BeneficiaryName) || string.IsNullOrWhiteSpace(model.AccountNumber))
            {
                TempData["ErrorMessage"] = "Beneficiary name and account number are required.";
                return RedirectToAction("Beneficiaries");
            }

            bool added = _db.AddBeneficiary(model);
            if (added)
            {
                TempData["SuccessMessage"] = $"Beneficiary '{model.BeneficiaryName}' added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add beneficiary.";
            }

            return RedirectToAction("Beneficiaries");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBeneficiary(int id)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            bool deleted = _db.DeleteBeneficiary(id, customerId);
            if (deleted)
            {
                TempData["SuccessMessage"] = "Beneficiary deleted.";
            }
            return RedirectToAction("Beneficiaries");
        }
    }
}
