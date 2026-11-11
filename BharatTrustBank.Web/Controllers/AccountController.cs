using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Services;
using BharatTrustBank.Web.Helpers;

namespace BharatTrustBank.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseHelper _db;

        public AccountController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null, string? role = null)
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
            {
                return RedirectUserToRoleDashboard(SessionHelper.GetUserRole(HttpContext.Session)!);
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                Role = !string.IsNullOrEmpty(role) ? role : "Customer"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = _db.AuthenticateUser(model.UsernameOrEmail.Trim(), model.Password, model.Role);
                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(model);
                }

                var user = result.User!;
                SessionHelper.SetUserSession(
                    HttpContext.Session,
                    user.UserId,
                    user.Username,
                    user.FullName,
                    user.Role,
                    result.CustomerId,
                    result.EmployeeId
                );

                TempData["SuccessMessage"] = $"Welcome back, {user.FullName}! Logged in as {user.Role}.";

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectUserToRoleDashboard(user.Role);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to the database. Please check your internet connection and try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
            {
                return RedirectUserToRoleDashboard(SessionHelper.GetUserRole(HttpContext.Session)!);
            }

            var model = new RegisterViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model, string? simulatedOtp)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Simulated OTP check (Accepts default '123456' or any 6-digit entered)
            if (string.IsNullOrEmpty(simulatedOtp) || simulatedOtp.Length != 6)
            {
                ModelState.AddModelError(string.Empty, "Invalid 6-digit verification OTP. For quick mode, enter 123456.");
                return View(model);
            }

            try
            {
                var result = _db.RegisterCustomer(model);
                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(model);
                }

                TempData["SuccessMessage"] = "Account created successfully! Please login with your new credentials.";
                return RedirectToAction("Login", new { role = "Customer" });
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to the database. Please check your internet connection and try again.");
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var username = SessionHelper.GetUsername(HttpContext.Session) ?? "User";
            var role = SessionHelper.GetUserRole(HttpContext.Session) ?? "General";

            try
            {
                if (userId.HasValue)
                {
                    _db.LogAudit(userId.Value, username, role, "Logout", "User logged out securely.");
                }
            }
            catch { /* If DB is unreachable, still clear the session */ }

            SessionHelper.ClearUserSession(HttpContext.Session);
            TempData["SuccessMessage"] = "You have been logged out securely.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            {
                return RedirectToAction("Login");
            }

            try
            {
                int? custId = SessionHelper.GetCustomerId(HttpContext.Session);
                if (custId.HasValue)
                {
                    var customer = _db.GetCustomerById(custId.Value);
                    return View(customer);
                }

                int userId = SessionHelper.GetUserId(HttpContext.Session)!.Value;
                var customerByUserId = _db.GetCustomerByUserId(userId);
                return View(customerByUserId);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Unable to load profile. Please check your internet connection.";
                return RedirectToAction("Index", "Home");
            }
        }

        private IActionResult RedirectUserToRoleDashboard(string role)
        {
            return role switch
            {
                "Admin" => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
                "Manager" => RedirectToAction("Index", "Dashboard", new { area = "Manager" }),
                "Employee" => RedirectToAction("Index", "Dashboard", new { area = "Employee" }),
                _ => RedirectToAction("Index", "Dashboard", new { area = "Customer" })
            };
        }
    }
}
