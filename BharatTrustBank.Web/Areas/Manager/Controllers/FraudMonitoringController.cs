using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Helpers;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Manager.Controllers
{
    [Area("Manager")]
    [RoleAuthorize("Manager", "Admin")]
    public class FraudMonitoringController : Controller
    {
        private readonly DatabaseHelper _db;

        public FraudMonitoringController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var vm = _db.GetManagerDashboard();
            return View(vm.BranchFraudAlerts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResolveAlert(int alertId, string status, string remarks)
        {
            int? mgrId = SessionHelper.GetEmployeeId(HttpContext.Session);
            string query = "UPDATE FraudAlerts SET Status = @Status, ReviewRemarks = @Remarks, ReviewedBy = @MgrId, ReviewedAt = CURRENT_TIMESTAMP WHERE AlertId = @AlertId";
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Remarks", remarks.Trim());
            cmd.Parameters.AddWithValue("@MgrId", (object?)mgrId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AlertId", alertId);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = $"Fraud Alert #{alertId} marked as '{status}'.";
            return RedirectToAction("Index");
        }
    }
}


