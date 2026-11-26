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
    public class CardsController : Controller
    {
        private readonly DatabaseHelper _db;

        public CardsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var debitCards = _db.GetDebitCardsByCustomerId(customerId);
            var creditCards = _db.GetCreditCardsByCustomerId(customerId);

            ViewBag.CreditCards = creditCards;
            return View(debitCards);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int cardId, string cardCategory, string currentStatus)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            string newStatus = currentStatus == "Active" ? "Blocked" : "Active";

            bool ok = _db.ToggleCardStatus(cardId, cardCategory, newStatus, customerId);
            if (ok)
            {
                TempData["SuccessMessage"] = $"Card has been {newStatus.ToLower()} successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update card status.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePIN(int cardId, string cardCategory, string newPin)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (string.IsNullOrEmpty(newPin) || newPin.Length != 4)
            {
                TempData["ErrorMessage"] = "ATM PIN must be exactly 4 digits.";
                return RedirectToAction("Index");
            }

            bool ok = _db.UpdateCardPIN(cardId, cardCategory, newPin, customerId);
            if (ok)
            {
                TempData["SuccessMessage"] = "ATM PIN updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update PIN.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateLimit(int cardId, string cardCategory, decimal limit)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (limit <= 0)
            {
                TempData["ErrorMessage"] = "Limit must be greater than zero.";
                return RedirectToAction("Index");
            }

            bool ok = _db.UpdateCardLimit(cardId, cardCategory, limit, customerId);
            if (ok)
            {
                TempData["SuccessMessage"] = $"Card spending limit updated to INR {limit:N0}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update limit.";
            }

            return RedirectToAction("Index");
        }
    }
}
