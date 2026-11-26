using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Helpers;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [RoleAuthorize("Customer")]
    public class InvestmentsController : Controller
    {
        private readonly DatabaseHelper _db;

        public InvestmentsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            var investments = _db.GetInvestmentsByCustomerId(customerId);

            ViewBag.Accounts = accounts;
            ViewBag.TotalInvested = investments.Sum(i => i.InvestedAmount);
            ViewBag.CurrentValue = investments.Sum(i => i.CurrentValue);
            ViewBag.TotalGain = (decimal)ViewBag.CurrentValue - (decimal)ViewBag.TotalInvested;

            return View(investments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Invest(int accountId, string investmentType, string schemeName, decimal amount)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (amount < 500)
            {
                TempData["ErrorMessage"] = "Minimum investment amount is INR 500.";
                return RedirectToAction("Index");
            }

            bool ok = _db.AddInvestment(customerId, accountId, investmentType, schemeName, amount);
            if (ok)
            {
                TempData["SuccessMessage"] = $"Investment of INR {amount:N2} in {schemeName} completed successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Insufficient balance or invalid account for investment.";
            }

            return RedirectToAction("Index");
        }
    }
}
