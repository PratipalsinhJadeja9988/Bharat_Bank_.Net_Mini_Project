using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using BharatTrustBank.Web.Filters;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Services;

namespace BharatTrustBank.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [RoleAuthorize("Admin")]
    public class AuditLogsController : Controller
    {
        private readonly DatabaseHelper _db;

        public AuditLogsController(DatabaseHelper db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var logs = new List<AuditLog>();
            using var conn = _db.GetConnection();
            conn.Open();
            string query = "SELECT LogId, UserId, Username, Role, Action, Description, IPAddress, Timestamp FROM AuditLogs ORDER BY Timestamp DESC LIMIT 100";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                logs.Add(new AuditLog
                {
                    LogId = r.GetInt32(0),
                    UserId = r.IsDBNull(1) ? null : r.GetInt32(1),
                    Username = r.GetString(2),
                    Role = r.GetString(3),
                    Action = r.GetString(4),
                    Description = r.GetString(5),
                    IPAddress = r.GetString(6),
                    Timestamp = r.GetDateTime(7)
                });
            }
            return View(logs);
        }
    }
}


