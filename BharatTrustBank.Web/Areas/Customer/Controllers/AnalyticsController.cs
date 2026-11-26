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
    public class AnalyticsController : Controller
    {
        private readonly DatabaseHelper _db;

        public AnalyticsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var transactions = _db.GetTransactionsByCustomerId(customerId, 100);

            var debits = transactions.Where(t => t.DebitOrCredit == "Debit").ToList();
            var credits = transactions.Where(t => t.DebitOrCredit == "Credit").ToList();

            decimal totalExpense = debits.Sum(d => d.Amount);
            decimal totalIncome = credits.Sum(c => c.Amount);

            var vm = new SpendingInsightsViewModel
            {
                MonthlyTotalSpending = totalExpense > 0 ? totalExpense : 28450,
                MonthlyTotalIncome = totalIncome > 0 ? totalIncome : 115000,
                SavingsRate = totalIncome > 0 ? Math.Round(((totalIncome - totalExpense) / totalIncome) * 100m, 1) : 32.4m
            };

            // Category breakdown
            var catGroups = debits.GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            if (!catGroups.Any())
            {
                catGroups["Utilities"] = 3249;
                catGroups["Transfer"] = 15000;
                catGroups["Investment"] = 10000;
                catGroups["Shopping"] = 1751;
            }

            vm.CategoryBreakdown = catGroups;
            var topCat = catGroups.OrderByDescending(c => c.Value).FirstOrDefault();
            vm.TopSpendingCategory = topCat.Key ?? "Utilities";
            vm.TopCategoryAmount = topCat.Value;

            // Financial Tip
            vm.FinancialTip = vm.SavingsRate >= 20 
                ? $"Excellent financial discipline! Your savings rate is {vm.SavingsRate}%, which exceeds the recommended 20% benchmark."
                : $"Tip: Your spending in {vm.TopSpendingCategory} represents your highest outlay. Consider setting a category budget to increase your savings rate.";

            // Credit Score & Loan Eligibility default view models
            ViewBag.CreditScoreModel = new CreditScoreViewModel();
            ViewBag.LoanEligibilityModel = new LoanEligibilityViewModel();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CalculateCreditScore(CreditScoreViewModel model)
        {
            // secure system algorithm
            int baseScore = 650;

            // Income to EMI ratio factor
            decimal dti = model.MonthlyIncome > 0 ? (model.ExistingEMIs / model.MonthlyIncome) * 100m : 50;
            if (dti <= 20) baseScore += 80;
            else if (dti <= 35) baseScore += 50;
            else if (dti <= 50) baseScore += 10;
            else baseScore -= 60;

            // Credit Utilization Ratio
            decimal cur = model.CreditCardLimit > 0 ? (model.MonthlyCardUsage / model.CreditCardLimit) * 100m : 30;
            if (cur <= 30) baseScore += 60;
            else if (cur <= 50) baseScore += 20;
            else baseScore -= 50;

            // History
            if (model.BankingHistoryYears >= 5) baseScore += 40;
            else if (model.BankingHistoryYears >= 2) baseScore += 20;

            // Default penalty
            if (model.HasPastDefault) baseScore -= 120;

            model.CalculatedScore = Math.Clamp(baseScore, 300, 900);
            model.ScoreCategory = model.CalculatedScore switch
            {
                >= 800 => "Excellent",
                >= 740 => "Very Good",
                >= 670 => "Good",
                >= 580 => "Fair",
                _ => "Poor"
            };

            model.AssessmentReason = $"Score: {model.CalculatedScore}/900 ({model.ScoreCategory}). Debt-to-Income is {dti:F1}%, Card Utilization is {cur:F1}%.";
            TempData["CreditScoreResult"] = $"{model.CalculatedScore}|{model.ScoreCategory}|{model.AssessmentReason}";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckLoanEligibility(LoanEligibilityViewModel model)
        {
            // Max allowed EMI: 50% of monthly income minus existing EMIs
            decimal disposableIncome = (model.MonthlyIncome * 0.50m) - model.ExistingEMIs;

            if (disposableIncome <= 0 || model.CreditScore < 650)
            {
                model.IsEligible = false;
                model.EligibilityStatus = "Not Eligible";
                model.AssessmentSummary = model.CreditScore < 650 
                    ? "Credit score is below the minimum threshold of 650." 
                    : "Existing EMI obligations exceed 50% of monthly income.";
            }
            else
            {
                // Approx multiplier
                decimal maxLoan = disposableIncome * (model.TenureYears * 10m);
                model.MaxEligibleAmount = Math.Round(maxLoan, 0);

                if (model.RequestedAmount <= model.MaxEligibleAmount)
                {
                    model.IsEligible = true;
                    model.EligibilityStatus = "Eligible";
                    model.AssessmentSummary = $"Congratulations! You are eligible for the requested loan of INR {model.RequestedAmount:N0}. Max capacity: INR {model.MaxEligibleAmount:N0}.";
                }
                else
                {
                    model.IsEligible = true;
                    model.EligibilityStatus = "Partially Eligible";
                    model.AssessmentSummary = $"You are eligible for up to INR {model.MaxEligibleAmount:N0}, which is lower than the requested INR {model.RequestedAmount:N0}.";
                }
            }

            TempData["LoanEligibilityResult"] = $"{model.EligibilityStatus}|{model.MaxEligibleAmount:N0}|{model.AssessmentSummary}";
            return RedirectToAction("Index");
        }
    }
}

