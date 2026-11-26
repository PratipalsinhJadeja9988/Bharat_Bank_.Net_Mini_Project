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
    public class DepositsController : Controller
    {
        private readonly DatabaseHelper _db;

        public DepositsController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult FD()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            var existingFds = _db.GetFixedDepositsByCustomerId(customerId);

            var model = new FDViewModel
            {
                AvailableAccounts = accounts,
                ExistingFDs = existingFds
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FD(FDViewModel model)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (!ModelState.IsValid)
            {
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                model.ExistingFDs = _db.GetFixedDepositsByCustomerId(customerId);
                return View(model);
            }

            var result = _db.CreateFixedDeposit(customerId, model.AccountId, model.PrincipalAmount, model.TenureMonths, model.AutoRenew, model.IsSeniorCitizen);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                model.ExistingFDs = _db.GetFixedDepositsByCustomerId(customerId);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("FD");
        }

        [HttpGet]
        public IActionResult RD()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            var existingRds = _db.GetRecurringDepositsByCustomerId(customerId);

            var model = new RDViewModel
            {
                AvailableAccounts = accounts,
                ExistingRDs = existingRds
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RD(RDViewModel model)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (!ModelState.IsValid)
            {
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                model.ExistingRDs = _db.GetRecurringDepositsByCustomerId(customerId);
                return View(model);
            }

            var result = _db.CreateRecurringDeposit(customerId, model.AccountId, model.MonthlyInstallment, model.TenureMonths);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                model.AvailableAccounts = _db.GetAccountsByCustomerId(customerId);
                model.ExistingRDs = _db.GetRecurringDepositsByCustomerId(customerId);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("RD");
        }
    }
}
