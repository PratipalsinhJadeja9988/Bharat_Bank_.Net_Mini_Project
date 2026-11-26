using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Helpers;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Manager.Controllers
{
    [Area("Manager")]
    [RoleAuthorize("Manager", "Admin")]
    public class LoanApprovalsController : Controller
    {
        private readonly DatabaseHelper _db;

        public LoanApprovalsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var vm = _db.GetManagerDashboard();
            return View(vm.PendingLoans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessLoan(int loanId, string status, string? remarks)
        {
            int? mgrId = SessionHelper.GetEmployeeId(HttpContext.Session);

            using var conn = _db.GetConnection();
            conn.Open();

            string query = @"
                UPDATE Loans 
                SET Status = @Status, Remarks = @Remarks, ApprovedBy = @MgrId, 
                    ApprovedDate = CASE WHEN @Status = 'Approved' THEN CURRENT_TIMESTAMP ELSE NULL END,
                    DisbursedDate = CASE WHEN @Status = 'Approved' THEN CURRENT_TIMESTAMP ELSE NULL END,
                    NextEMIDate = CASE WHEN @Status = 'Approved' THEN (CURRENT_TIMESTAMP + INTERVAL '30 days') ELSE NULL END
                WHERE LoanId = @LoanId";

            using (var cmd = new Npgsql.NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MgrId", (object?)mgrId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LoanId", loanId);
                cmd.ExecuteNonQuery();
            }

            // Notification to customer
            string notifQuery = @"
                INSERT INTO Notifications (UserId, Title, Message, NotificationType)
                SELECT c.UserId, 'Loan Application ' || @Status, 'Your loan application LOAN-#' || CAST(@LoanId AS TEXT) || ' has been ' || @Status || '.', 'General'
                FROM Loans l JOIN Customers c ON l.CustomerId = c.CustomerId WHERE l.LoanId = @LoanId";
            using (var notifCmd = new Npgsql.NpgsqlCommand(notifQuery, conn))
            {
                notifCmd.Parameters.AddWithValue("@Status", status);
                notifCmd.Parameters.AddWithValue("@LoanId", loanId);
                notifCmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = $"Loan application LOAN-#{loanId} marked as '{status}'.";
            return RedirectToAction("Index");
        }
    }
}


