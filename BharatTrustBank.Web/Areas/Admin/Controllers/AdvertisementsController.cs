using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [RoleAuthorize("Admin")]
    public class AdvertisementsController : Controller
    {
        private readonly DatabaseHelper _db;

        public AdvertisementsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var list = new List<Advertisement>();
            using var conn = _db.GetConnection();
            conn.Open();
            string query = "SELECT AdId, Title, Subtitle, TagText, BadgeColor, CallToActionText, TargetUrl, ImageUrl, DisplayOrder, IsActive, StartDate, EndDate FROM Advertisements ORDER BY DisplayOrder ASC";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Advertisement
                {
                    AdId = r.GetInt32(0),
                    Title = r.GetString(1),
                    Subtitle = r.GetString(2),
                    TagText = r.GetString(3),
                    BadgeColor = r.GetString(4),
                    CallToActionText = r.GetString(5),
                    TargetUrl = r.GetString(6),
                    ImageUrl = r.GetString(7),
                    DisplayOrder = r.GetInt32(8),
                    IsActive = r.GetInt32(9) == 1,
                    StartDate = r.GetDateTime(10),
                    EndDate = r.GetDateTime(11)
                });
            }
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Advertisement ad)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            string query = @"
                INSERT INTO Advertisements (Title, Subtitle, TagText, BadgeColor, CallToActionText, TargetUrl, ImageUrl, DisplayOrder, IsActive, StartDate, EndDate)
                VALUES (@Title, @Subtitle, @Tag, @Color, @CTA, @Target, @Img, @Order, 1, @Start, @End)";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Title", ad.Title.Trim());
            cmd.Parameters.AddWithValue("@Subtitle", ad.Subtitle.Trim());
            cmd.Parameters.AddWithValue("@Tag", ad.TagText.Trim());
            cmd.Parameters.AddWithValue("@Color", ad.BadgeColor.Trim());
            cmd.Parameters.AddWithValue("@CTA", ad.CallToActionText.Trim());
            cmd.Parameters.AddWithValue("@Target", ad.TargetUrl.Trim());
            cmd.Parameters.AddWithValue("@Img", ad.ImageUrl.Trim());
            cmd.Parameters.AddWithValue("@Order", ad.DisplayOrder);
            cmd.Parameters.AddWithValue("@Start", ad.StartDate);
            cmd.Parameters.AddWithValue("@End", ad.EndDate);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Promotional advertisement created successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Toggle(int id, bool currentStatus)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            string query = "UPDATE Advertisements SET IsActive = @Status WHERE AdId = @Id";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", !currentStatus ? 1 : 0);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Advertisement status toggled.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            string query = "DELETE FROM Advertisements WHERE AdId = @Id";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Advertisement deleted.";
            return RedirectToAction("Index");
        }
    }
}


