using System;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Helpers;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [RoleAuthorize("Customer")]
    public class ServicesController : Controller
    {
        private readonly DatabaseHelper _db;

        public ServicesController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            var accounts = _db.GetAccountsByCustomerId(customerId);
            var chequeRequests = _db.GetChequeRequestsByCustomerId(customerId);
            var serviceRequests = _db.GetServiceRequestsByCustomerId(customerId);
            var customer = _db.GetCustomerById(customerId);

            ViewBag.Accounts = accounts;
            ViewBag.ChequeRequests = chequeRequests;
            ViewBag.ServiceRequests = serviceRequests;
            ViewBag.Customer = customer;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestCheque(int accountId, int numberOfLeaves, string deliveryAddress)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                TempData["ErrorMessage"] = "Delivery address is required.";
                return RedirectToAction("Index");
            }

            bool ok = _db.RequestChequeBook(customerId, accountId, numberOfLeaves, deliveryAddress);
            if (ok)
            {
                TempData["SuccessMessage"] = $"Cheque book request for {numberOfLeaves} leaves placed successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to place cheque book request.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitServiceRequest(string requestType, string? oldValue, string newValue, string? reason)
        {
            int customerId = SessionHelper.GetCustomerId(HttpContext.Session)!.Value;
            if (string.IsNullOrWhiteSpace(newValue))
            {
                TempData["ErrorMessage"] = "New updated value cannot be empty.";
                return RedirectToAction("Index");
            }

            bool ok = _db.CreateServiceRequest(customerId, requestType, oldValue, newValue, reason);
            if (ok)
            {
                TempData["SuccessMessage"] = $"Service Request for '{requestType}' submitted for Branch Verification.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to submit service request.";
            }

            return RedirectToAction("Index");
        }
    }
}
