using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Helpers;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Employee.Controllers
{
    [Area("Employee")]
    [RoleAuthorize("Employee", "Manager", "Admin")]
    public class RequestsController : Controller
    {
        private readonly DatabaseHelper _db;

        public RequestsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult ChequeRequests()
        {
            var vm = _db.GetEmployeeDashboard();
            return View(vm.PendingChequeRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateChequeStatus(int requestId, string status, string? remarks)
        {
            int? empId = SessionHelper.GetEmployeeId(HttpContext.Session);
            string query = "UPDATE ChequeRequests SET Status = @Status, Remarks = @Remarks, ProcessedBy = @EmpId, ProcessedDate = CURRENT_TIMESTAMP WHERE RequestId = @Id";
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmpId", (object?)empId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", requestId);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = $"Cheque request updated to '{status}'.";
            return RedirectToAction("ChequeRequests");
        }

        public IActionResult ServiceRequests()
        {
            var vm = _db.GetEmployeeDashboard();
            return View(vm.PendingServiceRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateServiceRequest(int serviceRequestId, string status, string? remarks)
        {
            int? empId = SessionHelper.GetEmployeeId(HttpContext.Session);
            string query = "UPDATE ServiceRequests SET Status = @Status, Remarks = @Remarks, ProcessedBy = @EmpId, ProcessedDate = CURRENT_TIMESTAMP WHERE ServiceRequestId = @Id";
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EmpId", (object?)empId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", serviceRequestId);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = $"Service request updated to '{status}'.";
            return RedirectToAction("ServiceRequests");
        }
    }
}


