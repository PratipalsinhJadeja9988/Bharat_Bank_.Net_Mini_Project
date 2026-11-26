using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Employee.Controllers
{
    [Area("Employee")]
    [RoleAuthorize("Employee", "Manager", "Admin")]
    public class DashboardController : Controller
    {
        private readonly DatabaseHelper _db;

        public DashboardController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var vm = _db.GetEmployeeDashboard();
            return View(vm);
        }
    }
}
