using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Manager.Controllers
{
    [Area("Manager")]
    [RoleAuthorize("Manager", "Admin")]
    public class AccountsController : Controller
    {
        private readonly DatabaseHelper _db;

        public AccountsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Inactive()
        {
            var vm = _db.GetManagerDashboard();
            return View(vm.InactiveAccounts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAccountStatus(int accountId, string newStatus)
        {
            string query = "UPDATE Accounts SET Status = @Status, LastActivityDate = CURRENT_TIMESTAMP WHERE AccountId = @AccountId";
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", newStatus);
            cmd.Parameters.AddWithValue("@AccountId", accountId);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = $"Account status updated to '{newStatus}'.";
            return RedirectToAction("Inactive");
        }
    }
}


