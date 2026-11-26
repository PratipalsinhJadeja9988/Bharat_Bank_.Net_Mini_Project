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
    public class LoansController : Controller
    {
        private readonly DatabaseHelper _db;

        public LoansController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var loans = _db.GetLoansByCustomerId(customerId);

            var model = new LoanApplyViewModel
            {
                ExistingLoans = loans
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Apply(LoanApplyViewModel model)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (!ModelState.IsValid)
            {
                model.ExistingLoans = _db.GetLoansByCustomerId(customerId);
                return View("Index", model);
            }

            // Set standard rates based on category
            model.InterestRate = model.LoanType switch
            {
                "Home Loan" => 8.50m,
                "Vehicle Loan" => 8.90m,
                "Education Loan" => 9.25m,
                _ => 10.75m
            };

            var result = _db.ApplyForLoan(customerId, model);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                model.ExistingLoans = _db.GetLoansByCustomerId(customerId);
                return View("Index", model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index");
        }
    }
}
