using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BharatTrustBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly string _connectionString;

        public LoansController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpGet]
        public IActionResult GetLoans([FromQuery] int? customerId)
        {
            var list = new List<object>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string query = @"
                SELECT l.LoanId, l.CustomerId, u.FullName, l.LoanType, l.LoanAmount, l.TenureMonths, l.InterestRate, l.MonthlyEMI, l.Status, l.OutstandingBalance, l.CreatedAt
                FROM Loans l
                JOIN Customers c ON l.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId " +
                (customerId.HasValue ? "WHERE l.CustomerId = @CustId " : "") +
                "ORDER BY l.CreatedAt DESC";

            using var cmd = new NpgsqlCommand(query, conn);
            if (customerId.HasValue) cmd.Parameters.AddWithValue("@CustId", customerId.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new
                {
                    LoanId = r.GetInt32(0),
                    CustomerId = r.GetInt32(1),
                    CustomerName = r.GetString(2),
                    LoanType = r.GetString(3),
                    LoanAmount = r.GetDecimal(4),
                    TenureMonths = r.GetInt32(5),
                    InterestRate = r.GetDecimal(6),
                    MonthlyEMI = r.GetDecimal(7),
                    Status = r.IsDBNull(8) ? null : r.GetString(8),
                    OutstandingBalance = r.GetDecimal(9),
                    CreatedAt = r.GetDateTime(10)
                });
            }
            return Ok(list);
        }
    }
}
