using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Manager.Controllers
{
    [Area("Manager")]
    [RoleAuthorize("Manager", "Admin")]
    public class ReportsController : Controller
    {
        private readonly DatabaseHelper _db;

        public ReportsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index(string reportType = "Customers", DateTime? fromDate = null, DateTime? toDate = null)
        {
            DataTable dt;
            if (reportType == "Transactions")
            {
                dt = _db.GetTransactionReportDataTable(fromDate, toDate);
            }
            else
            {
                dt = _db.GetCustomerReportDataTable(fromDate, toDate);
            }

            ViewBag.ReportType = reportType;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            return View(dt);
        }
    }
}
