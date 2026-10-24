using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BharatTrustBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly string _connectionString;

        public AccountsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpGet]
        public IActionResult GetAccounts([FromQuery] int? customerId)
        {
            var list = new List<object>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string query = @"
                SELECT a.AccountId, a.CustomerId, a.AccountNumber, t.TypeName, a.Balance, a.Currency, a.Status, a.IFSCCode, a.BranchName, a.OpenedDate
                FROM Accounts a
                JOIN AccountTypes t ON a.AccountTypeId = t.AccountTypeId " +
                (customerId.HasValue ? "WHERE a.CustomerId = @CustId " : "");

            using var cmd = new NpgsqlCommand(query, conn);
            if (customerId.HasValue) cmd.Parameters.AddWithValue("@CustId", customerId.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new
                {
                    AccountId = r.GetInt32(0),
                    CustomerId = r.GetInt32(1),
                    AccountNumber = r.GetString(2),
                    AccountType = r.GetString(3),
                    Balance = r.GetDecimal(4),
                    Currency = r.IsDBNull(5) ? null : r.GetString(5),
                    Status = r.IsDBNull(6) ? null : r.GetString(6),
                    IFSCCode = r.IsDBNull(7) ? null : r.GetString(7),
                    BranchName = r.IsDBNull(8) ? null : r.GetString(8),
                    OpenedDate = r.GetDateTime(9)
                });
            }
            return Ok(list);
        }

        [HttpGet("{id}")]
        public IActionResult GetAccountById(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string query = @"
                SELECT a.AccountId, a.CustomerId, a.AccountNumber, t.TypeName, a.Balance, a.Currency, a.Status, a.IFSCCode, a.BranchName, a.OpenedDate, u.FullName
                FROM Accounts a
                JOIN AccountTypes t ON a.AccountTypeId = t.AccountTypeId
                JOIN Customers c ON a.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE a.AccountId = @Id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                var acc = new
                {
                    AccountId = r.GetInt32(0),
                    CustomerId = r.GetInt32(1),
                    AccountNumber = r.GetString(2),
                    AccountType = r.GetString(3),
                    Balance = r.GetDecimal(4),
                    Currency = r.IsDBNull(5) ? null : r.GetString(5),
                    Status = r.IsDBNull(6) ? null : r.GetString(6),
                    IFSCCode = r.IsDBNull(7) ? null : r.GetString(7),
                    BranchName = r.IsDBNull(8) ? null : r.GetString(8),
                    OpenedDate = r.GetDateTime(9),
                    CustomerName = r.GetString(10)
                };
                return Ok(acc);
            }
            return NotFound(new { Message = $"Account #{id} not found." });
        }
    }
}
