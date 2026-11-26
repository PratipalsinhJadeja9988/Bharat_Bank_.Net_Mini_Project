using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [RoleAuthorize("Admin")]
    public class SettingsController : Controller
    {
        private readonly DatabaseHelper _db;

        public SettingsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var settings = new Dictionary<string, string>();
            using var conn = _db.GetConnection();
            conn.Open();
            string query = "SELECT SettingKey, SettingValue FROM SystemSettings";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                settings[r.GetString(0)] = r.GetString(1);
            }
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Dictionary<string, string> settings)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            foreach (var kvp in settings)
            {
                string query = "UPDATE SystemSettings SET SettingValue = @Val, LastUpdated = CURRENT_TIMESTAMP WHERE SettingKey = @Key";
                using var cmd = new Npgsql.NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Val", kvp.Value.Trim());
                cmd.Parameters.AddWithValue("@Key", kvp.Key);
                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "System configuration updated successfully.";
            return RedirectToAction("Index");
        }
    }
}


