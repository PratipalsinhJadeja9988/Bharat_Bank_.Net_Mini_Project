using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [RoleAuthorize("Admin")]
    public class ContentController : Controller
    {
        private readonly DatabaseHelper _db;

        public ContentController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var faqs = _db.GetActiveFAQs();
            var offers = _db.GetActiveOffers();
            var currencies = _db.GetCurrencyRates();

            ViewBag.Offers = offers;
            ViewBag.Currencies = currencies;
            return View(faqs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateFAQ(FAQ faq)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            string query = "INSERT INTO FAQs (Category, Question, Answer, DisplayOrder, IsActive) VALUES (@Category, @Question, @Answer, @Order, 1)";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Category", faq.Category);
            cmd.Parameters.AddWithValue("@Question", faq.Question.Trim());
            cmd.Parameters.AddWithValue("@Answer", faq.Answer.Trim());
            cmd.Parameters.AddWithValue("@Order", faq.DisplayOrder);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "FAQ published successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateOffer(Offer offer)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            string query = "INSERT INTO Offers (Title, Description, Category, DiscountCode, ValidTill, IsActive) VALUES (@Title, @Desc, @Cat, @Code, @ValidTill, 1)";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Title", offer.Title.Trim());
            cmd.Parameters.AddWithValue("@Desc", offer.Description.Trim());
            cmd.Parameters.AddWithValue("@Cat", offer.Category.Trim());
            cmd.Parameters.AddWithValue("@Code", (object?)offer.DiscountCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ValidTill", offer.ValidTill);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Promotional offer published successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCurrency(int currencyId, decimal buyRate, decimal sellRate)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            string query = "UPDATE CurrencyRates SET BuyRate = @Buy, SellRate = @Sell, LastUpdated = CURRENT_TIMESTAMP WHERE CurrencyId = @Id";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Buy", buyRate);
            cmd.Parameters.AddWithValue("@Sell", sellRate);
            cmd.Parameters.AddWithValue("@Id", currencyId);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Currency exchange rate updated.";
            return RedirectToAction("Index");
        }
    }
}


