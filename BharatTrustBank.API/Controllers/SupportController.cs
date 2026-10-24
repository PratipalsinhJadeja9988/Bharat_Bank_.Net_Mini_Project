using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BharatTrustBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupportController : ControllerBase
    {
        private readonly string _connectionString;

        public SupportController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpGet("faqs")]
        public IActionResult GetFAQs()
        {
            var list = new List<object>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string query = "SELECT FAQId, Category, Question, Answer, DisplayOrder FROM FAQs WHERE IsActive = 1 ORDER BY DisplayOrder ASC";
            using var cmd = new NpgsqlCommand(query, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new
                {
                    FAQId = r.GetInt32(0),
                    Category = r.GetString(1),
                    Question = r.GetString(2),
                    Answer = r.GetString(3),
                    DisplayOrder = r.IsDBNull(4) ? 1 : r.GetInt32(4)
                });
            }
            return Ok(list);
        }

        [HttpPost("ticket")]
        public IActionResult CreateTicket([FromBody] ApiTicketRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Subject) || string.IsNullOrWhiteSpace(req.Message))
            {
                return BadRequest(new { Message = "Subject and Message are required." });
            }

            string ticketNo = "TCK" + DateTime.Now.ToString("yyyy") + new Random().Next(1000, 9999);
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string query = @"
                INSERT INTO SupportTickets (CustomerId, TicketNumber, Subject, Category, Message, Priority, Status, CreatedAt)
                VALUES (@CustomerId, @TicketNumber, @Subject, @Category, @Message, @Priority, 'Open', CURRENT_TIMESTAMP)";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", req.CustomerId);
            cmd.Parameters.AddWithValue("@TicketNumber", ticketNo);
            cmd.Parameters.AddWithValue("@Subject", req.Subject.Trim());
            cmd.Parameters.AddWithValue("@Category", req.Category ?? "General");
            cmd.Parameters.AddWithValue("@Message", req.Message.Trim());
            cmd.Parameters.AddWithValue("@Priority", req.Priority ?? "Normal");
            cmd.ExecuteNonQuery();

            return Ok(new
            {
                Success = true,
                Message = "Support ticket submitted successfully.",
                TicketNumber = ticketNo
            });
        }
    }

    public class ApiTicketRequest
    {
        public int CustomerId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? Category { get; set; } = "General";
        public string Message { get; set; } = string.Empty;
        public string? Priority { get; set; } = "Normal";
    }
}
