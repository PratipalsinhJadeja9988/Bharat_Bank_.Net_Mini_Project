using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Manager.Controllers
{
    [Area("Manager")]
    [RoleAuthorize("Manager", "Admin")]
    public class InterestRatesController : Controller
    {
        private readonly DatabaseHelper _db;

        public InterestRatesController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var rates = _db.GetInterestRates();
            return View(rates);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateRate(int rateId, decimal regularRate, decimal seniorCitizenRate)
        {
            string query = "UPDATE InterestRates SET RegularRate = @Reg, SeniorCitizenRate = @Sr, LastUpdated = CURRENT_TIMESTAMP WHERE RateId = @Id";
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Reg", regularRate);
            cmd.Parameters.AddWithValue("@Sr", seniorCitizenRate);
            cmd.Parameters.AddWithValue("@Id", rateId);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Interest rate updated successfully.";
            return RedirectToAction("Index");
        }
    }
}


