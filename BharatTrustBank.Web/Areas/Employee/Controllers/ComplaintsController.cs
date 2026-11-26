using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Helpers;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Employee.Controllers
{
    [Area("Employee")]
    [RoleAuthorize("Employee", "Manager", "Admin")]
    public class ComplaintsController : Controller
    {
        private readonly DatabaseHelper _db;

        public ComplaintsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var vm = _db.GetEmployeeDashboard();
            return View(vm.AssignedTickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResolveTicket(int ticketId, string status, string resolution)
        {
            int? empId = SessionHelper.GetEmployeeId(HttpContext.Session);
            string query = "UPDATE SupportTickets SET Status = @Status, Resolution = @Resolution, AssignedEmployeeId = @EmpId, ResolvedAt = CURRENT_TIMESTAMP WHERE TicketId = @Id";
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Resolution", resolution.Trim());
            cmd.Parameters.AddWithValue("@EmpId", (object?)empId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", ticketId);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = $"Support ticket updated to '{status}'.";
            return RedirectToAction("Index");
        }
    }
}


