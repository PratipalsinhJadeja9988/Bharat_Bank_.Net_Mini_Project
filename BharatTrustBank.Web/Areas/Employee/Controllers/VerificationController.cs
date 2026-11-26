using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Helpers;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Employee.Controllers
{
    [Area("Employee")]
    [RoleAuthorize("Employee", "Manager", "Admin")]
    public class VerificationController : Controller
    {
        private readonly DatabaseHelper _db;

        public VerificationController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult KYC()
        {
            var vm = _db.GetEmployeeDashboard();
            return View(vm.PendingKYCCustomers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateKYC(int customerId, string status, string? remarks)
        {
            int? empId = SessionHelper.GetEmployeeId(HttpContext.Session);
            string query = @"
                UPDATE Customers 
                SET KYCStatus = @Status, KYCVerifiedBy = @EmpId, KYCVerifiedDate = CURRENT_TIMESTAMP, KYCRemarks = @Remarks
                WHERE CustomerId = @CustomerId";

            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@EmpId", (object?)empId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.ExecuteNonQuery();

            // Notification to customer
            string notifQuery = @"
                INSERT INTO Notifications (UserId, Title, Message, NotificationType)
                SELECT UserId, 'KYC Status Update', 'Your KYC verification status has been marked as ' || @Status || '.', 'Security'
                FROM Customers WHERE CustomerId = @CustomerId";
            using var notifCmd = new Npgsql.NpgsqlCommand(notifQuery, conn);
            notifCmd.Parameters.AddWithValue("@Status", status);
            notifCmd.Parameters.AddWithValue("@CustomerId", customerId);
            notifCmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = $"Customer KYC marked as '{status}'.";
            return RedirectToAction("KYC");
        }
    }
}


