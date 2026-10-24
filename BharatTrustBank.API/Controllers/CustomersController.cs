using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BharatTrustBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly string _connectionString;

        public CustomersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpGet]
        public IActionResult GetCustomers()
        {
            var list = new List<object>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string query = @"
                SELECT c.CustomerId, c.CustomerCode, u.FullName, u.Email, u.MobileNumber, c.PAN, c.KYCStatus, c.Segment, c.CreatedAt
                FROM Customers c JOIN Users u ON c.UserId = u.UserId";
            using var cmd = new NpgsqlCommand(query, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new
                {
                    CustomerId = r.GetInt32(0),
                    CustomerCode = r.GetString(1),
                    FullName = r.GetString(2),
                    Email = r.GetString(3),
                    MobileNumber = r.GetString(4),
                    PAN = r.GetString(5),
                    KYCStatus = r.IsDBNull(6) ? null : r.GetString(6),
                    Segment = r.IsDBNull(7) ? null : r.GetString(7),
                    CreatedAt = r.GetDateTime(8)
                });
            }
            return Ok(list);
        }

        [HttpGet("{id}")]
        public IActionResult GetCustomer(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string query = @"
                SELECT c.CustomerId, c.CustomerCode, u.FullName, u.Email, u.MobileNumber, c.Address, c.City, c.State, c.Pincode, c.PAN, c.Aadhaar, c.KYCStatus, c.Segment, c.CreatedAt
                FROM Customers c JOIN Users u ON c.UserId = u.UserId WHERE c.CustomerId = @Id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                var customer = new
                {
                    CustomerId = r.GetInt32(0),
                    CustomerCode = r.GetString(1),
                    FullName = r.GetString(2),
                    Email = r.GetString(3),
                    MobileNumber = r.GetString(4),
                    Address = r.GetString(5),
                    City = r.GetString(6),
                    State = r.GetString(7),
                    Pincode = r.GetString(8),
                    PAN = r.GetString(9),
                    Aadhaar = r.GetString(10),
                    KYCStatus = r.IsDBNull(11) ? null : r.GetString(11),
                    Segment = r.IsDBNull(12) ? null : r.GetString(12),
                    CreatedAt = r.GetDateTime(13)
                };
                return Ok(customer);
            }
            return NotFound(new { Message = $"Customer with ID {id} not found." });
        }
    }
}
