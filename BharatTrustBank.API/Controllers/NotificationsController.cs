using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BharatTrustBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly string _connectionString;

        public NotificationsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpGet]
        public IActionResult GetNotifications([FromQuery] int userId)
        {
            var list = new List<object>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string query = "SELECT NotificationId, UserId, Title, Message, NotificationType, IsRead, CreatedAt FROM Notifications WHERE UserId = @UserId ORDER BY CreatedAt DESC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new
                {
                    NotificationId = r.GetInt32(0),
                    UserId = r.GetInt32(1),
                    Title = r.GetString(2),
                    Message = r.GetString(3),
                    NotificationType = r.IsDBNull(4) ? "General" : r.GetString(4),
                    IsRead = r.GetInt32(5) == 1,
                    CreatedAt = r.GetDateTime(6)
                });
            }
            return Ok(list);
        }
    }
}
