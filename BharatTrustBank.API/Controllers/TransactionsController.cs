using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BharatTrustBank.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly string _connectionString;

        public TransactionsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpGet]
        public IActionResult GetTransactions([FromQuery] int? accountId, [FromQuery] int limit = 50)
        {
            var list = new List<object>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string query = @"
                SELECT t.TransactionId, t.TransactionReference, t.AccountId, a.AccountNumber, t.TransactionType,
                                   t.Amount, t.BalanceAfter, t.DebitOrCredit, t.Category, t.Description,
                                   t.DestinationAccount, t.BeneficiaryName, t.PaymentMethod, t.Status, t.TransactionDate
                FROM Transactions t JOIN Accounts a ON t.AccountId = a.AccountId " +
                (accountId.HasValue ? "WHERE t.AccountId = @AccId " : "") +
                "ORDER BY t.TransactionDate DESC LIMIT @Limit";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Limit", limit);
            if (accountId.HasValue) cmd.Parameters.AddWithValue("@AccId", accountId.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new
                {
                    TransactionId = r.GetInt32(0),
                    TransactionReference = r.GetString(1),
                    AccountId = r.GetInt32(2),
                    AccountNumber = r.GetString(3),
                    TransactionType = r.GetString(4),
                    Amount = r.GetDecimal(5),
                    BalanceAfter = r.GetDecimal(6),
                    DebitOrCredit = r.GetString(7),
                    Category = r.GetString(8),
                    Description = r.GetString(9),
                    DestinationAccount = r.IsDBNull(10) ? null : r.GetString(10),
                    BeneficiaryName = r.IsDBNull(11) ? null : r.GetString(11),
                    PaymentMethod = r.IsDBNull(12) ? "Online" : r.GetString(12),
                    Status = r.IsDBNull(13) ? "Completed" : r.GetString(13),
                    TransactionDate = r.GetDateTime(14)
                });
            }
            return Ok(list);
        }

        [HttpPost("transfer")]
        public IActionResult ExecuteTransfer([FromBody] ApiTransferRequest req)
        {
            if (req.Amount <= 0) return BadRequest(new { Message = "Transfer amount must be positive." });

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // Get Sender Account
                using var cmdSender = new NpgsqlCommand("SELECT Balance, AccountNumber FROM Accounts WHERE AccountId = @Id FOR UPDATE", conn, tx);
                cmdSender.Parameters.AddWithValue("@Id", req.FromAccountId);
                
                decimal senderBalance = 0;
                string senderAccountNumber = "";
                using (var r = cmdSender.ExecuteReader())
                {
                    if (!r.Read()) return BadRequest(new { Message = "Sender account not found." });
                    senderBalance = r.GetDecimal(0);
                    senderAccountNumber = r.GetString(1);
                }

                if (senderBalance < req.Amount)
                {
                    return BadRequest(new { Message = "Insufficient funds." });
                }

                string transactionRef = "TRX" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999);
                decimal newSenderBalance = senderBalance - req.Amount;

                // Deduct from Sender
                using var cmdUpdateSender = new NpgsqlCommand("UPDATE Accounts SET Balance = @Bal WHERE AccountId = @Id", conn, tx);
                cmdUpdateSender.Parameters.AddWithValue("@Bal", newSenderBalance);
                cmdUpdateSender.Parameters.AddWithValue("@Id", req.FromAccountId);
                cmdUpdateSender.ExecuteNonQuery();

                // Insert Sender Transaction
                using var cmdTxSender = new NpgsqlCommand(@"
                    INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status, TransactionDate)
                    VALUES (@Ref, @AccId, @Type, @Amt, @Bal, 'Debit', 'Transfer', @Desc, @Dest, @Ben, 'Online', 'Completed', CURRENT_TIMESTAMP)", conn, tx);
                cmdTxSender.Parameters.AddWithValue("@Ref", transactionRef);
                cmdTxSender.Parameters.AddWithValue("@AccId", req.FromAccountId);
                cmdTxSender.Parameters.AddWithValue("@Type", req.TransferType ?? "IMPS");
                cmdTxSender.Parameters.AddWithValue("@Amt", req.Amount);
                cmdTxSender.Parameters.AddWithValue("@Bal", newSenderBalance);
                cmdTxSender.Parameters.AddWithValue("@Desc", req.Description ?? "REST API Fund Transfer");
                cmdTxSender.Parameters.AddWithValue("@Dest", (object?)req.ToAccountNumber ?? DBNull.Value);
                cmdTxSender.Parameters.AddWithValue("@Ben", (object?)req.BeneficiaryName ?? DBNull.Value);
                cmdTxSender.ExecuteNonQuery();

                // Check Receiver Account
                using var cmdReceiver = new NpgsqlCommand("SELECT AccountId, Balance FROM Accounts WHERE AccountNumber = @AccNum FOR UPDATE", conn, tx);
                cmdReceiver.Parameters.AddWithValue("@AccNum", req.ToAccountNumber);
                int? receiverId = null;
                decimal receiverBalance = 0;

                using (var r = cmdReceiver.ExecuteReader())
                {
                    if (r.Read())
                    {
                        receiverId = r.GetInt32(0);
                        receiverBalance = r.GetDecimal(1);
                    }
                }

                if (receiverId.HasValue)
                {
                    decimal newReceiverBalance = receiverBalance + req.Amount;
                    
                    using var cmdUpdateReceiver = new NpgsqlCommand("UPDATE Accounts SET Balance = @Bal WHERE AccountId = @Id", conn, tx);
                    cmdUpdateReceiver.Parameters.AddWithValue("@Bal", newReceiverBalance);
                    cmdUpdateReceiver.Parameters.AddWithValue("@Id", receiverId.Value);
                    cmdUpdateReceiver.ExecuteNonQuery();

                    using var cmdTxReceiver = new NpgsqlCommand(@"
                        INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status, TransactionDate)
                        VALUES (@Ref, @AccId, @Type, @Amt, @Bal, 'Credit', 'Transfer', @Desc, @Dest, @Ben, 'Online', 'Completed', CURRENT_TIMESTAMP)", conn, tx);
                    cmdTxReceiver.Parameters.AddWithValue("@Ref", transactionRef + "-R");
                    cmdTxReceiver.Parameters.AddWithValue("@AccId", receiverId.Value);
                    cmdTxReceiver.Parameters.AddWithValue("@Type", req.TransferType ?? "IMPS");
                    cmdTxReceiver.Parameters.AddWithValue("@Amt", req.Amount);
                    cmdTxReceiver.Parameters.AddWithValue("@Bal", newReceiverBalance);
                    cmdTxReceiver.Parameters.AddWithValue("@Desc", "Received from " + senderAccountNumber);
                    cmdTxReceiver.Parameters.AddWithValue("@Dest", senderAccountNumber);
                    cmdTxReceiver.Parameters.AddWithValue("@Ben", "");
                    cmdTxReceiver.ExecuteNonQuery();
                }

                tx.Commit();

                return Ok(new
                {
                    Success = true,
                    Message = "Transfer successful.",
                    TransactionReference = transactionRef,
                    Amount = req.Amount
                });
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return BadRequest(new { Success = false, Message = "Transfer failed: " + ex.Message });
            }
        }
    }

    public class ApiTransferRequest
    {
        public int FromAccountId { get; set; }
        public string ToAccountNumber { get; set; } = string.Empty;
        public string BeneficiaryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransferType { get; set; } = "IMPS";
        public string? Description { get; set; } = "API Transfer";
    }
}
