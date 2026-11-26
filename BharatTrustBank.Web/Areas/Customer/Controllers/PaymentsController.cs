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
    public class PaymentsController : Controller
    {
        private readonly DatabaseHelper _db;

        public PaymentsController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index(string? category = null)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            var history = _db.GetBillPaymentsByCustomerId(customerId);

            var model = new BillPayViewModel
            {
                BillerCategory = !string.IsNullOrEmpty(category) ? category : "Electricity",
                BillerName = category == "Mobile" ? "Jio Prepaid" : (category == "Gas" ? "Mahanagar Gas" : "Adani Electricity"),
                AvailableAccounts = accounts,
                PaymentHistory = history
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(BillPayViewModel model)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (!ModelState.IsValid)
            {
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                model.PaymentHistory = _db.GetBillPaymentsByCustomerId(customerId);
                return View(model);
            }

            var result = _db.ProcessBillPayment(model, customerId);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                model.PaymentHistory = _db.GetBillPaymentsByCustomerId(customerId);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index", new { category = model.BillerCategory });
        }

        public IActionResult QR()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            ViewBag.Accounts = accounts;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessQRPayment(int accountId, string merchantName, decimal amount, string otp)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (string.IsNullOrEmpty(otp) || otp.Length != 6)
            {
                TempData["ErrorMessage"] = "Invalid simulated OTP. Please enter 123456.";
                return RedirectToAction("QR");
            }

            var billModel = new BillPayViewModel
            {
                AccountId = accountId,
                BillerCategory = "QR Payment",
                BillerName = merchantName,
                ConsumerNumber = "MERCHANT-UPI-QR",
                Amount = amount
            };

            var res = _db.ProcessBillPayment(billModel, customerId);
            if (res.Success)
            {
                TempData["SuccessMessage"] = $"QR Payment of INR {amount:N2} to {merchantName} completed successfully! Ref: {res.Reference}";
            }
            else
            {
                TempData["ErrorMessage"] = res.Message;
            }

            return RedirectToAction("QR");
        }
    }
}
