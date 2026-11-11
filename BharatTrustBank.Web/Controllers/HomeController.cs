using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseHelper _db;

        public HomeController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            try
            {
                ViewBag.Advertisements = _db.GetActiveAdvertisements();
                ViewBag.Offers = _db.GetActiveOffers();
                ViewBag.InterestRates = _db.GetInterestRates();
                ViewBag.Currencies = _db.GetCurrencyRates();
            }
            catch (Exception ex)
            {
                ViewBag.Advertisements = new List<Advertisement>();
                ViewBag.Offers = new List<Offer>();
                ViewBag.InterestRates = new List<InterestRate>();
                ViewBag.Currencies = new List<CurrencyRate>();
                ViewBag.DbError = "Could not load live data: " + ex.Message.Split('\n')[0];
            }
            return View();
        }

        public IActionResult Summary()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AcceptTerms()
        {
            HttpContext.Session.SetString("BTB_TermsAccepted", "true");
            TempData["SuccessMessage"] = "Terms & Conditions accepted. Please proceed to Login or Registration.";
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Rates()
        {
            try
            {
                var rates = _db.GetInterestRates();
                ViewBag.Currencies = _db.GetCurrencyRates();
                return View(rates);
            }
            catch
            {
                ViewBag.Currencies = new List<CurrencyRate>();
                return View(new List<InterestRate>());
            }
        }

        public IActionResult Offers()
        {
            try
            {
                return View(_db.GetActiveOffers());
            }
            catch
            {
                return View(new List<Offer>());
            }
        }

        public IActionResult FAQ(string? category = null)
        {
            try
            {
                var faqs = _db.GetActiveFAQs(category);
                ViewBag.SelectedCategory = category ?? "All";
                return View(faqs);
            }
            catch
            {
                ViewBag.SelectedCategory = category ?? "All";
                return View(new List<FAQ>());
            }
        }

        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
