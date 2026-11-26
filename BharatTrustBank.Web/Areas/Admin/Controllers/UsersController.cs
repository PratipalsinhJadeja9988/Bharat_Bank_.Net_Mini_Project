using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Helpers;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [RoleAuthorize("Admin")]
    public class UsersController : Controller
    {
        private readonly DatabaseHelper _db;

        public UsersController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index(string? role = null)
        {
            var list = new List<User>();
            using var conn = _db.GetConnection();
            conn.Open();

            string query = "SELECT UserId, Username, Email, PasswordHash, Salt, Role, FullName, MobileNumber, IsActive, IsLocked, FailedLoginAttempts, LockoutEnd, CreatedAt, LastLoginAt FROM Users " +
                (!string.IsNullOrEmpty(role) ? "WHERE Role = @Role " : "") +
                "ORDER BY UserId DESC";

            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            if (!string.IsNullOrEmpty(role)) cmd.Parameters.AddWithValue("@Role", role);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new User
                {
                    UserId = r.GetInt32(0),
                    Username = r.GetString(1),
                    Email = r.GetString(2),
                    PasswordHash = r.GetString(3),
                    Salt = r.GetString(4),
                    Role = r.GetString(5),
                    FullName = r.GetString(6),
                    MobileNumber = r.GetString(7),
                    IsActive = r.GetInt32(8) == 1,
                    IsLocked = r.GetInt32(9) == 1,
                    FailedLoginAttempts = r.GetInt32(10),
                    LockoutEnd = r.IsDBNull(11) ? null : r.GetDateTime(11),
                    CreatedAt = r.GetDateTime(12),
                    LastLoginAt = r.IsDBNull(13) ? null : r.GetDateTime(13)
                });
            }

            ViewBag.SelectedRole = role ?? "All";
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleUserActive(int userId, bool currentActive)
        {
            bool newActive = !currentActive;
            string query = "UPDATE Users SET IsActive = @Active WHERE UserId = @Id";
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Active", newActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@Id", userId);
            cmd.ExecuteNonQuery();

            _db.LogAudit(SessionHelper.GetUserId(HttpContext.Session), SessionHelper.GetUsername(HttpContext.Session) ?? "Admin", "Admin", "User Status Toggle", $"User ID {userId} set to Active: {newActive}");
            TempData["SuccessMessage"] = $"User status updated to {(newActive ? "Active" : "Inactive")}.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnlockUser(int userId)
        {
            string query = "UPDATE Users SET IsLocked = 0, FailedLoginAttempts = 0, LockoutEnd = NULL WHERE UserId = @Id";
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            cmd.ExecuteNonQuery();

            _db.LogAudit(SessionHelper.GetUserId(HttpContext.Session), SessionHelper.GetUsername(HttpContext.Session) ?? "Admin", "Admin", "User Unlock", $"Account lockout cleared for User ID {userId}");
            TempData["SuccessMessage"] = "User account unlocked successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateStaff(string username, string email, string password, string fullName, string mobileNumber, string role, string department, string designation, decimal salary)
        {
            string salt = PasswordHelper.GenerateSalt();
            string hash = PasswordHelper.HashPassword(password, salt);

            using var conn = _db.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                string userQuery = @"
                    INSERT INTO Users (Username, Email, PasswordHash, Salt, Role, FullName, MobileNumber, IsActive, CreatedAt)
                    VALUES (@Username, @Email, @Hash, @Salt, @Role, @FullName, @Mobile, 1, CURRENT_TIMESTAMP)
                    RETURNING UserId";
                int newUserId;
                using (var cmd = new Npgsql.NpgsqlCommand(userQuery, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Username", username.Trim());
                    cmd.Parameters.AddWithValue("@Email", email.Trim());
                    cmd.Parameters.AddWithValue("@Hash", hash);
                    cmd.Parameters.AddWithValue("@Salt", salt);
                    cmd.Parameters.AddWithValue("@Role", role);
                    cmd.Parameters.AddWithValue("@FullName", fullName.Trim());
                    cmd.Parameters.AddWithValue("@Mobile", mobileNumber.Trim());
                    newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string empCode = "EMP" + (1000 + newUserId).ToString();
                string empQuery = @"
                    INSERT INTO Employees (UserId, EmployeeCode, Department, Designation, BranchName, DateOfJoining, Salary)
                    VALUES (@UserId, @Code, @Dept, @Desig, 'Main Branch - Mumbai', CURRENT_TIMESTAMP, @Salary)";
                using (var empCmd = new Npgsql.NpgsqlCommand(empQuery, conn, tx))
                {
                    empCmd.Parameters.AddWithValue("@UserId", newUserId);
                    empCmd.Parameters.AddWithValue("@Code", empCode);
                    empCmd.Parameters.AddWithValue("@Dept", department.Trim());
                    empCmd.Parameters.AddWithValue("@Desig", designation.Trim());
                    empCmd.Parameters.AddWithValue("@Salary", salary);
                    empCmd.ExecuteNonQuery();
                }

                tx.Commit();
                TempData["SuccessMessage"] = $"New {role} account '{username}' created successfully.";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                TempData["ErrorMessage"] = "Error creating staff: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}


