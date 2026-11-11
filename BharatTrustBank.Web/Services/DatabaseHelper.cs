using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using Microsoft.Extensions.Configuration;
using BharatTrustBank.Web.Models;
using BharatTrustBank.Web.Helpers;

namespace BharatTrustBank.Web.Services
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Database=BharatTrustBankDB;Username=postgres;Password=postgres;";
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        #region Generic ADO.NET Helpers

        public int ExecuteNonQuery(string query, NpgsqlParameter[]? parameters = null, CommandType commandType = CommandType.Text)
        {
            using var conn = GetConnection();
            using var cmd = new NpgsqlCommand(query, conn) { CommandType = commandType };
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        public object? ExecuteScalar(string query, NpgsqlParameter[]? parameters = null, CommandType commandType = CommandType.Text)
        {
            using var conn = GetConnection();
            using var cmd = new NpgsqlCommand(query, conn) { CommandType = commandType };
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteScalar();
        }

        public DataTable ExecuteDataTable(string query, NpgsqlParameter[]? parameters = null, CommandType commandType = CommandType.Text)
        {
            using var conn = GetConnection();
            using var cmd = new NpgsqlCommand(query, conn) { CommandType = commandType };
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(reader);
            return dt;
        }

        #endregion

        #region Authentication & User Management

        public (bool Success, string Message, User? User, int? CustomerId, int? EmployeeId) AuthenticateUser(string usernameOrEmail, string password, string requestedRole)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = "SELECT UserId, Username, Email, PasswordHash, Salt, Role, FullName, MobileNumber, IsActive, IsLocked, FailedLoginAttempts, LockoutEnd FROM Users WHERE (Username = @Input OR Email = @Input)";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Input", usernameOrEmail);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return (false, "Invalid username or password.", null, null, null);
            }

            var user = new User
            {
                UserId = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                Salt = reader.GetString(4),
                Role = reader.GetString(5),
                FullName = reader.GetString(6),
                MobileNumber = reader.GetString(7),
                IsActive = reader.GetInt32(8) == 1,
                IsLocked = reader.GetInt32(9) == 1,
                FailedLoginAttempts = reader.GetInt32(10),
                LockoutEnd = reader.IsDBNull(11) ? null : reader.GetDateTime(11)
            };
            reader.Close();

            // Check if user is active
            if (!user.IsActive)
            {
                LogLoginHistory(user.UserId, false, "Account is deactivated.");
                return (false, "Your account has been deactivated. Please contact customer support.", null, null, null);
            }

            // Check if locked
            if (user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            {
                LogLoginHistory(user.UserId, false, "Account locked due to multiple failed attempts.");
                return (false, $"Account is temporarily locked until {user.LockoutEnd.Value:hh:mm tt} due to multiple failed login attempts.", null, null, null);
            }

            // Validate requested role against database role
            if (!string.Equals(user.Role, requestedRole, StringComparison.OrdinalIgnoreCase))
            {
                LogLoginHistory(user.UserId, false, $"Role mismatch. Selected: {requestedRole}, Actual: {user.Role}");
                return (false, $"Access Denied: You cannot login as '{requestedRole}' with these credentials.", null, null, null);
            }

            // Verify Password
            bool passwordValid = PasswordHelper.VerifyPassword(password, user.PasswordHash, user.Salt);
            if (!passwordValid)
            {
                int newFailedAttempts = user.FailedLoginAttempts + 1;
                bool shouldLock = newFailedAttempts >= 5;
                DateTime? lockoutEnd = shouldLock ? DateTime.Now.AddMinutes(15) : null;

                string updateQuery = "UPDATE Users SET FailedLoginAttempts = @Failed, IsLocked = @Locked, LockoutEnd = @LockEnd WHERE UserId = @UserId";
                using var updateCmd = new NpgsqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@Failed", newFailedAttempts);
                updateCmd.Parameters.AddWithValue("@Locked", shouldLock ? 1 : 0);
                updateCmd.Parameters.AddWithValue("@LockEnd", (object?)lockoutEnd ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@UserId", user.UserId);
                updateCmd.ExecuteNonQuery();

                LogLoginHistory(user.UserId, false, "Incorrect password.");
                string msg = shouldLock ? "Account locked for 15 minutes due to 5 consecutive failed attempts." : $"Incorrect password. Attempt {newFailedAttempts} of 5.";
                return (false, msg, null, null, null);
            }

            // Upgrade legacy live hashes on successful login, then reset failed-login state.
            bool needsRehash = PasswordHelper.NeedsRehash(user.PasswordHash);
            string newSalt = needsRehash ? PasswordHelper.GenerateSalt() : user.Salt;
            string newHash = needsRehash ? PasswordHelper.HashPassword(password, newSalt) : user.PasswordHash;
            string resetQuery = "UPDATE Users SET FailedLoginAttempts = 0, IsLocked = 0, LockoutEnd = NULL, LastLoginAt = CURRENT_TIMESTAMP, PasswordHash = @PasswordHash, Salt = @Salt WHERE UserId = @UserId";
            using (var resetCmd = new NpgsqlCommand(resetQuery, conn))
            {
                resetCmd.Parameters.AddWithValue("@UserId", user.UserId);
                resetCmd.Parameters.AddWithValue("@PasswordHash", newHash);
                resetCmd.Parameters.AddWithValue("@Salt", newSalt);
                resetCmd.ExecuteNonQuery();
            }

            LogLoginHistory(user.UserId, true, "Successful login.");
            LogAudit(user.UserId, user.Username, user.Role, "Login", "User successfully authenticated into " + user.Role + " portal.");

            // Fetch specific Role IDs
            int? customerId = null;
            int? employeeId = null;

            if (user.Role == "Customer")
            {
                string custQuery = "SELECT CustomerId FROM Customers WHERE UserId = @UserId";
                using var custCmd = new NpgsqlCommand(custQuery, conn);
                custCmd.Parameters.AddWithValue("@UserId", user.UserId);
                var res = custCmd.ExecuteScalar();
                if (res != null && res != DBNull.Value) customerId = Convert.ToInt32(res);
            }
            else if (user.Role == "Employee" || user.Role == "Manager")
            {
                string empQuery = "SELECT EmployeeId FROM Employees WHERE UserId = @UserId";
                using var empCmd = new NpgsqlCommand(empQuery, conn);
                empCmd.Parameters.AddWithValue("@UserId", user.UserId);
                var res = empCmd.ExecuteScalar();
                if (res != null && res != DBNull.Value) employeeId = Convert.ToInt32(res);
            }

            return (true, "Login successful.", user, customerId, employeeId);
        }

        public (bool Success, string Message, int UserId) RegisterCustomer(RegisterViewModel model)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // Check if username or email already exists
                string checkQuery = "SELECT COUNT(1) FROM Users WHERE Username = @Username OR Email = @Email";
                using (var checkCmd = new NpgsqlCommand(checkQuery, conn, tx))
                {
                    checkCmd.Parameters.AddWithValue("@Username", model.Username.Trim());
                    checkCmd.Parameters.AddWithValue("@Email", model.Email.Trim());
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        tx.Rollback();
                        return (false, "Username or Email already registered with BharatTrust Bank.", 0);
                    }
                }

                // Create User
                string salt = PasswordHelper.GenerateSalt();
                string passwordHash = PasswordHelper.HashPassword(model.Password, salt);

                string insertUserQuery = @"
                    INSERT INTO Users (Username, Email, PasswordHash, Salt, Role, FullName, MobileNumber, IsActive, CreatedAt)
                    VALUES (@Username, @Email, @PasswordHash, @Salt, 'Customer', @FullName, @MobileNumber, 1, CURRENT_TIMESTAMP)
                    RETURNING UserId";

                int userId;
                using (var userCmd = new NpgsqlCommand(insertUserQuery, conn, tx))
                {
                    userCmd.Parameters.AddWithValue("@Username", model.Username.Trim());
                    userCmd.Parameters.AddWithValue("@Email", model.Email.Trim());
                    userCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    userCmd.Parameters.AddWithValue("@Salt", salt);
                    userCmd.Parameters.AddWithValue("@FullName", model.FullName.Trim());
                    userCmd.Parameters.AddWithValue("@MobileNumber", model.MobileNumber.Trim());
                    userId = Convert.ToInt32(userCmd.ExecuteScalar());
                }

                // Create Customer
                string customerCode = "CUST" + (100000 + userId).ToString();
                string insertCustomerQuery = @"
                    INSERT INTO Customers (UserId, CustomerCode, DateOfBirth, Gender, Address, City, State, Pincode, PAN, Aadhaar, KYCStatus, NomineeName, NomineeRelation, CreatedAt)
                    VALUES (@UserId, @CustomerCode, @DOB, @Gender, @Address, @City, @State, @Pincode, @PAN, @Aadhaar, 'Verified', @NomineeName, @NomineeRelation, CURRENT_TIMESTAMP)
                    RETURNING CustomerId";

                int customerId;
                using (var custCmd = new NpgsqlCommand(insertCustomerQuery, conn, tx))
                {
                    custCmd.Parameters.AddWithValue("@UserId", userId);
                    custCmd.Parameters.AddWithValue("@CustomerCode", customerCode);
                    custCmd.Parameters.AddWithValue("@DOB", model.DateOfBirth);
                    custCmd.Parameters.AddWithValue("@Gender", model.Gender);
                    custCmd.Parameters.AddWithValue("@Address", model.Address.Trim());
                    custCmd.Parameters.AddWithValue("@City", model.City.Trim());
                    custCmd.Parameters.AddWithValue("@State", model.State.Trim());
                    custCmd.Parameters.AddWithValue("@Pincode", model.Pincode.Trim());
                    custCmd.Parameters.AddWithValue("@PAN", model.PAN.Trim().ToUpper());
                    custCmd.Parameters.AddWithValue("@Aadhaar", model.Aadhaar.Trim());
                    custCmd.Parameters.AddWithValue("@NomineeName", (object?)model.NomineeName ?? DBNull.Value);
                    custCmd.Parameters.AddWithValue("@NomineeRelation", (object?)model.NomineeRelation ?? DBNull.Value);
                    customerId = Convert.ToInt32(custCmd.ExecuteScalar());
                }

                // Open Initial Primary Account
                string accountNumber = "100100" + (100000 + customerId).ToString();
                string insertAccountQuery = @"
                    INSERT INTO Accounts (CustomerId, AccountNumber, AccountTypeId, Balance, Currency, Status, IFSCCode, BranchName, OpenedDate)
                    VALUES (@CustomerId, @AccountNumber, @AccountTypeId, 25000.00, 'INR', 'Active', 'BHTT0001001', 'Main Branch, Nariman Point, Mumbai', CURRENT_TIMESTAMP)
                    RETURNING AccountId";

                int accountId;
                using (var accCmd = new NpgsqlCommand(insertAccountQuery, conn, tx))
                {
                    accCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    accCmd.Parameters.AddWithValue("@AccountNumber", accountNumber);
                    accCmd.Parameters.AddWithValue("@AccountTypeId", model.AccountTypeId);
                    accountId = Convert.ToInt32(accCmd.ExecuteScalar());
                }

                // Initial Welcome Credit Transaction
                string insertTxnQuery = @"
                    INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, Status, TransactionDate)
                    VALUES (@TxnRef, @AccountId, 'Deposit', 25000.00, 25000.00, 'Credit', 'Cash', 'Welcome Account Opening Balance', 'Completed', CURRENT_TIMESTAMP)";
                using (var txnCmd = new NpgsqlCommand(insertTxnQuery, conn, tx))
                {
                    txnCmd.Parameters.AddWithValue("@TxnRef", "TXN" + DateTime.Now.ToString("yyyyMMdd") + (1000 + userId));
                    txnCmd.Parameters.AddWithValue("@AccountId", accountId);
                    txnCmd.ExecuteNonQuery();
                }

                // Generate Instant Virtual RuPay Debit Card
                string cardNumber = "4532 9801 " + (1000 + customerId).ToString() + " " + (2000 + customerId).ToString();
                string insertCardQuery = @"
                    INSERT INTO DebitCards (CustomerId, AccountId, CardNumber, CardHolderName, CardType, ExpiryDate, CVV, PINHash, DailyLimit, Status)
                    VALUES (@CustomerId, @AccountId, @CardNumber, @CardHolder, 'RuPay Platinum', '08/30', '432', '1234', 50000.00, 'Active')";
                using (var cardCmd = new NpgsqlCommand(insertCardQuery, conn, tx))
                {
                    cardCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    cardCmd.Parameters.AddWithValue("@AccountId", accountId);
                    cardCmd.Parameters.AddWithValue("@CardNumber", cardNumber);
                    cardCmd.Parameters.AddWithValue("@CardHolder", model.FullName.Trim().ToUpper());
                    cardCmd.ExecuteNonQuery();
                }

                // Welcome Notification
                string notifQuery = "INSERT INTO Notifications (UserId, Title, Message, NotificationType) VALUES (@UserId, 'Welcome to BharatTrust Bank', 'Your new account " + accountNumber + " is active. Welcome bonus of INR 25,000 credited.', 'General')";
                using (var notifCmd = new NpgsqlCommand(notifQuery, conn, tx))
                {
                    notifCmd.Parameters.AddWithValue("@UserId", userId);
                    notifCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return (true, "Registration successful! You can now log in.", userId);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, "Registration error: " + ex.Message, 0);
            }
        }

        public void LogLoginHistory(int userId, bool isSuccess, string? failureReason = null)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string query = "INSERT INTO LoginHistory (UserId, IPAddress, Browser, DeviceType, IsSuccess, FailureReason, LoginTime) VALUES (@UserId, '127.0.0.1', 'Chrome / Desktop', 'Windows PC', @IsSuccess, @FailureReason, CURRENT_TIMESTAMP)";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@IsSuccess", isSuccess ? 1 : 0);
                cmd.Parameters.AddWithValue("@FailureReason", (object?)failureReason ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public void LogAudit(int? userId, string username, string role, string action, string description)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string query = "INSERT INTO AuditLogs (UserId, Username, Role, Action, Description, IPAddress, Timestamp) VALUES (@UserId, @Username, @Role, @Action, @Description, '127.0.0.1', CURRENT_TIMESTAMP)";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@Description", description);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        #endregion

        #region Customer & Accounts Data Access

        public Customer? GetCustomerByUserId(int userId)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT c.CustomerId, c.UserId, c.CustomerCode, c.DateOfBirth, c.Gender, c.Address, c.City, c.State, c.Pincode,
                       c.PAN, c.Aadhaar, c.KYCStatus, c.KYCRemarks, c.NomineeName, c.NomineeRelation, c.Segment, c.CreatedAt,
                       u.FullName, u.Email, u.MobileNumber, u.Username
                FROM Customers c
                JOIN Users u ON c.UserId = u.UserId
                WHERE c.UserId = @UserId";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Customer
                {
                    CustomerId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    CustomerCode = reader.GetString(2),
                    DateOfBirth = reader.GetDateTime(3),
                    Gender = reader.GetString(4),
                    Address = reader.GetString(5),
                    City = reader.GetString(6),
                    State = reader.GetString(7),
                    Pincode = reader.GetString(8),
                    PAN = reader.GetString(9),
                    Aadhaar = reader.GetString(10),
                    KYCStatus = reader.GetString(11),
                    KYCRemarks = reader.IsDBNull(12) ? null : reader.GetString(12),
                    NomineeName = reader.IsDBNull(13) ? null : reader.GetString(13),
                    NomineeRelation = reader.IsDBNull(14) ? null : reader.GetString(14),
                    Segment = reader.GetString(15),
                    CreatedAt = reader.GetDateTime(16),
                    FullName = reader.GetString(17),
                    Email = reader.GetString(18),
                    MobileNumber = reader.GetString(19),
                    Username = reader.GetString(20)
                };
            }
            return null;
        }

        public Customer? GetCustomerById(int customerId)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT c.CustomerId, c.UserId, c.CustomerCode, c.DateOfBirth, c.Gender, c.Address, c.City, c.State, c.Pincode,
                       c.PAN, c.Aadhaar, c.KYCStatus, c.KYCRemarks, c.NomineeName, c.NomineeRelation, c.Segment, c.CreatedAt,
                       u.FullName, u.Email, u.MobileNumber, u.Username
                FROM Customers c
                JOIN Users u ON c.UserId = u.UserId
                WHERE c.CustomerId = @CustomerId";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Customer
                {
                    CustomerId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    CustomerCode = reader.GetString(2),
                    DateOfBirth = reader.GetDateTime(3),
                    Gender = reader.GetString(4),
                    Address = reader.GetString(5),
                    City = reader.GetString(6),
                    State = reader.GetString(7),
                    Pincode = reader.GetString(8),
                    PAN = reader.GetString(9),
                    Aadhaar = reader.GetString(10),
                    KYCStatus = reader.GetString(11),
                    KYCRemarks = reader.IsDBNull(12) ? null : reader.GetString(12),
                    NomineeName = reader.IsDBNull(13) ? null : reader.GetString(13),
                    NomineeRelation = reader.IsDBNull(14) ? null : reader.GetString(14),
                    Segment = reader.GetString(15),
                    CreatedAt = reader.GetDateTime(16),
                    FullName = reader.GetString(17),
                    Email = reader.GetString(18),
                    MobileNumber = reader.GetString(19),
                    Username = reader.GetString(20)
                };
            }
            return null;
        }

        public List<Account> GetAccountsByCustomerId(int customerId)
        {
            var list = new List<Account>();
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT a.AccountId, a.CustomerId, a.AccountNumber, a.AccountTypeId, t.TypeName, a.Balance, a.Currency, a.Status, a.IFSCCode, a.BranchName, a.OpenedDate, a.LastActivityDate
                FROM Accounts a
                JOIN AccountTypes t ON a.AccountTypeId = t.AccountTypeId
                WHERE a.CustomerId = @CustomerId";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Account
                {
                    AccountId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    AccountNumber = reader.GetString(2),
                    AccountTypeId = reader.GetInt32(3),
                    AccountTypeName = reader.GetString(4),
                    Balance = reader.GetDecimal(5),
                    Currency = reader.GetString(6),
                    Status = reader.GetString(7),
                    IFSCCode = reader.GetString(8),
                    BranchName = reader.GetString(9),
                    OpenedDate = reader.GetDateTime(10),
                    LastActivityDate = reader.GetDateTime(11)
                });
            }
            return list;
        }

        public Account? GetAccountById(int accountId)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT a.AccountId, a.CustomerId, a.AccountNumber, a.AccountTypeId, t.TypeName, a.Balance, a.Currency, a.Status, a.IFSCCode, a.BranchName, a.OpenedDate, a.LastActivityDate, u.FullName
                FROM Accounts a
                JOIN AccountTypes t ON a.AccountTypeId = t.AccountTypeId
                JOIN Customers c ON a.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE a.AccountId = @AccountId";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@AccountId", accountId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Account
                {
                    AccountId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    AccountNumber = reader.GetString(2),
                    AccountTypeId = reader.GetInt32(3),
                    AccountTypeName = reader.GetString(4),
                    Balance = reader.GetDecimal(5),
                    Currency = reader.GetString(6),
                    Status = reader.GetString(7),
                    IFSCCode = reader.GetString(8),
                    BranchName = reader.GetString(9),
                    OpenedDate = reader.GetDateTime(10),
                    LastActivityDate = reader.GetDateTime(11),
                    CustomerName = reader.GetString(12)
                };
            }
            return null;
        }

        private bool AccountBelongsToCustomer(int accountId, int customerId)
        {
            const string query = "SELECT COUNT(1) FROM Accounts WHERE AccountId = @AccountId AND CustomerId = @CustomerId";
            using var conn = GetConnection();
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@AccountId", accountId);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
        }

        public List<Transaction> GetTransactionsByCustomerId(int customerId, int limit = 50, string? category = null)
        {
            var list = new List<Transaction>();
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT t.TransactionId, t.TransactionReference, t.AccountId, a.AccountNumber, t.TransactionType,
                                   t.Amount, t.BalanceAfter, t.DebitOrCredit, t.Category, t.Description,
                                   t.DestinationAccount, t.BeneficiaryName, t.PaymentMethod, t.Status, t.TransactionDate
                FROM Transactions t
                JOIN Accounts a ON t.AccountId = a.AccountId
                WHERE a.CustomerId = @CustomerId " +
                (!string.IsNullOrEmpty(category) ? "AND t.Category = @Category " : "") +
                "ORDER BY t.TransactionDate DESC, t.TransactionId DESC LIMIT @Limit";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Limit", limit);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            if (!string.IsNullOrEmpty(category)) cmd.Parameters.AddWithValue("@Category", category);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Transaction
                {
                    TransactionId = reader.GetInt32(0),
                    TransactionReference = reader.GetString(1),
                    AccountId = reader.GetInt32(2),
                    AccountNumber = reader.GetString(3),
                    TransactionType = reader.GetString(4),
                    Amount = reader.GetDecimal(5),
                    BalanceAfter = reader.GetDecimal(6),
                    DebitOrCredit = reader.GetString(7),
                    Category = reader.GetString(8),
                    Description = reader.GetString(9),
                    DestinationAccount = reader.IsDBNull(10) ? null : reader.GetString(10),
                    BeneficiaryName = reader.IsDBNull(11) ? null : reader.GetString(11),
                    PaymentMethod = reader.GetString(12),
                    Status = reader.GetString(13),
                    TransactionDate = reader.GetDateTime(14)
                });
            }
            return list;
        }

        #endregion

        #region Banking Transactions (Transfer, Deposit, Withdraw)

        public (bool Success, string Message, string Reference) ExecuteFundTransfer(int customerId, int fromAccountId, string toAccount, string beneficiaryName, decimal amount, string transferType, string description)
        {
            if (!AccountBelongsToCustomer(fromAccountId, customerId)) return (false, "Selected account is not available.", "");
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try {
                // Check balance
                using var cmdCheck = new NpgsqlCommand("SELECT Balance FROM Accounts WHERE AccountId = @AccountId", conn, tx);
                cmdCheck.Parameters.AddWithValue("@AccountId", fromAccountId);
                var balObj = cmdCheck.ExecuteScalar();
                if (balObj == null) return (false, "Account not found.", "");
                decimal balance = Convert.ToDecimal(balObj);
                if (balance < amount) return (false, "Insufficient balance.", "");
                
                string txnRef = "TXN" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                
                // Deduct from sender
                using var cmdDeduct = new NpgsqlCommand("UPDATE Accounts SET Balance = Balance - @Amount WHERE AccountId = @AccountId", conn, tx);
                cmdDeduct.Parameters.AddWithValue("@Amount", amount);
                cmdDeduct.Parameters.AddWithValue("@AccountId", fromAccountId);
                cmdDeduct.ExecuteNonQuery();
                
                decimal newBalance = balance - amount;
                
                // Insert sender txn
                using var cmdTxn = new NpgsqlCommand("INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status) VALUES (@Ref, @Acc, 'Transfer', @Amt, @Bal, 'Debit', 'Transfer', @Desc, @Dest, @BenName, @Method, 'Completed')", conn, tx);
                cmdTxn.Parameters.AddWithValue("@Ref", txnRef);
                cmdTxn.Parameters.AddWithValue("@Acc", fromAccountId);
                cmdTxn.Parameters.AddWithValue("@Amt", amount);
                cmdTxn.Parameters.AddWithValue("@Bal", newBalance);
                cmdTxn.Parameters.AddWithValue("@Desc", description);
                cmdTxn.Parameters.AddWithValue("@Dest", toAccount);
                cmdTxn.Parameters.AddWithValue("@BenName", beneficiaryName);
                cmdTxn.Parameters.AddWithValue("@Method", transferType);
                cmdTxn.ExecuteNonQuery();
                
                // Check if ToAccount is internal
                using var cmdCheckTo = new NpgsqlCommand("SELECT AccountId, Balance FROM Accounts WHERE AccountNumber = @AccNum", conn, tx);
                cmdCheckTo.Parameters.AddWithValue("@AccNum", toAccount);
                using var reader = cmdCheckTo.ExecuteReader();
                if (reader.Read()) {
                    int toId = reader.GetInt32(0);
                    decimal toBal = reader.GetDecimal(1);
                    reader.Close();
                    
                    // Add to receiver
                    using var cmdAdd = new NpgsqlCommand("UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountId = @AccountId", conn, tx);
                    cmdAdd.Parameters.AddWithValue("@Amount", amount);
                    cmdAdd.Parameters.AddWithValue("@AccountId", toId);
                    cmdAdd.ExecuteNonQuery();
                    
                    // Insert receiver txn
                    using var cmdTxn2 = new NpgsqlCommand("INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status) VALUES (@Ref, @Acc, 'Transfer', @Amt, @Bal, 'Credit', 'Transfer', @Desc, @Dest, @BenName, @Method, 'Completed')", conn, tx);
                    cmdTxn2.Parameters.AddWithValue("@Ref", txnRef + "R");
                    cmdTxn2.Parameters.AddWithValue("@Acc", toId);
                    cmdTxn2.Parameters.AddWithValue("@Amt", amount);
                    cmdTxn2.Parameters.AddWithValue("@Bal", toBal + amount);
                    cmdTxn2.Parameters.AddWithValue("@Desc", "Received: " + description);
                    cmdTxn2.Parameters.AddWithValue("@Dest", "Self");
                    cmdTxn2.Parameters.AddWithValue("@BenName", "Sender");
                    cmdTxn2.Parameters.AddWithValue("@Method", transferType);
                    cmdTxn2.ExecuteNonQuery();
                } else {
                    reader.Close();
                }
                
                tx.Commit();
                return (true, "Transaction processed successfully.", txnRef);
            } catch (Exception ex) {
                tx.Rollback();
                return (false, "Transaction failed: " + ex.Message, "");
            }
        }

        public (bool Success, string Message, string Reference) ExecuteDeposit(int customerId, int accountId, decimal amount, string description)
        {
            if (!AccountBelongsToCustomer(accountId, customerId)) return (false, "Selected account is not available.", "");
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try {
                using var cmdCheck = new NpgsqlCommand("SELECT Balance FROM Accounts WHERE AccountId = @AccountId", conn, tx);
                cmdCheck.Parameters.AddWithValue("@AccountId", accountId);
                decimal balance = Convert.ToDecimal(cmdCheck.ExecuteScalar());
                
                using var cmdAdd = new NpgsqlCommand("UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountId = @AccountId", conn, tx);
                cmdAdd.Parameters.AddWithValue("@Amount", amount);
                cmdAdd.Parameters.AddWithValue("@AccountId", accountId);
                cmdAdd.ExecuteNonQuery();
                
                decimal newBalance = balance + amount;
                string txnRef = "DEP" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                
                using var cmdTxn = new NpgsqlCommand("INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status) VALUES (@Ref, @Acc, 'Deposit', @Amt, @Bal, 'Credit', 'Deposit', @Desc, 'Self', 'Self', 'Online', 'Completed')", conn, tx);
                cmdTxn.Parameters.AddWithValue("@Ref", txnRef);
                cmdTxn.Parameters.AddWithValue("@Acc", accountId);
                cmdTxn.Parameters.AddWithValue("@Amt", amount);
                cmdTxn.Parameters.AddWithValue("@Bal", newBalance);
                cmdTxn.Parameters.AddWithValue("@Desc", description);
                cmdTxn.ExecuteNonQuery();
                
                tx.Commit();
                return (true, "Deposit processed.", txnRef);
            } catch (Exception ex) {
                tx.Rollback();
                return (false, "Deposit failed: " + ex.Message, "");
            }
        }

        public (bool Success, string Message, string Reference) ExecuteWithdrawal(int customerId, int accountId, decimal amount, string description)
        {
            if (!AccountBelongsToCustomer(accountId, customerId)) return (false, "Selected account is not available.", "");
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try {
                using var cmdCheck = new NpgsqlCommand("SELECT Balance FROM Accounts WHERE AccountId = @AccountId", conn, tx);
                cmdCheck.Parameters.AddWithValue("@AccountId", accountId);
                decimal balance = Convert.ToDecimal(cmdCheck.ExecuteScalar());
                if (balance < amount) return (false, "Insufficient balance.", "");
                
                using var cmdDeduct = new NpgsqlCommand("UPDATE Accounts SET Balance = Balance - @Amount WHERE AccountId = @AccountId", conn, tx);
                cmdDeduct.Parameters.AddWithValue("@Amount", amount);
                cmdDeduct.Parameters.AddWithValue("@AccountId", accountId);
                cmdDeduct.ExecuteNonQuery();
                
                decimal newBalance = balance - amount;
                string txnRef = "WDL" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                
                using var cmdTxn = new NpgsqlCommand("INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status) VALUES (@Ref, @Acc, 'Withdrawal', @Amt, @Bal, 'Debit', 'Withdrawal', @Desc, 'Self', 'Self', 'Online', 'Completed')", conn, tx);
                cmdTxn.Parameters.AddWithValue("@Ref", txnRef);
                cmdTxn.Parameters.AddWithValue("@Acc", accountId);
                cmdTxn.Parameters.AddWithValue("@Amt", amount);
                cmdTxn.Parameters.AddWithValue("@Bal", newBalance);
                cmdTxn.Parameters.AddWithValue("@Desc", description);
                cmdTxn.ExecuteNonQuery();
                
                tx.Commit();
                return (true, "Withdrawal processed.", txnRef);
            } catch (Exception ex) {
                tx.Rollback();
                return (false, "Withdrawal failed: " + ex.Message, "");
            }
        }

        #endregion

        #region Beneficiaries Management

        public List<Beneficiary> GetBeneficiariesByCustomerId(int customerId)
        {
            var list = new List<Beneficiary>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT BeneficiaryId, CustomerId, BeneficiaryName, AccountNumber, BankName, IFSCCode, TransferType, UPIId, MaxLimit, IsActive, AddedDate FROM Beneficiaries WHERE CustomerId = @CustomerId AND IsActive = 1 ORDER BY BeneficiaryName";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Beneficiary
                {
                    BeneficiaryId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    BeneficiaryName = reader.GetString(2),
                    AccountNumber = reader.GetString(3),
                    BankName = reader.GetString(4),
                    IFSCCode = reader.GetString(5),
                    TransferType = reader.GetString(6),
                    UPIId = reader.IsDBNull(7) ? null : reader.GetString(7),
                    MaxLimit = reader.GetDecimal(8),
                    IsActive = reader.GetInt32(9) == 1,
                    AddedDate = reader.GetDateTime(10)
                });
            }
            return list;
        }

        public bool AddBeneficiary(Beneficiary b)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                INSERT INTO Beneficiaries (CustomerId, BeneficiaryName, AccountNumber, BankName, IFSCCode, TransferType, UPIId, MaxLimit, IsActive, AddedDate)
                VALUES (@CustomerId, @Name, @AccNum, @Bank, @IFSC, @Type, @UPI, @Limit, 1, CURRENT_TIMESTAMP)";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", b.CustomerId);
            cmd.Parameters.AddWithValue("@Name", b.BeneficiaryName.Trim());
            cmd.Parameters.AddWithValue("@AccNum", b.AccountNumber.Trim());
            cmd.Parameters.AddWithValue("@Bank", b.BankName.Trim());
            cmd.Parameters.AddWithValue("@IFSC", b.IFSCCode.Trim().ToUpper());
            cmd.Parameters.AddWithValue("@Type", b.TransferType);
            cmd.Parameters.AddWithValue("@UPI", (object?)b.UPIId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Limit", b.MaxLimit);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeleteBeneficiary(int beneficiaryId, int customerId)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = "UPDATE Beneficiaries SET IsActive = 0 WHERE BeneficiaryId = @Id AND CustomerId = @CustomerId";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", beneficiaryId);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            return cmd.ExecuteNonQuery() > 0;
        }

        #endregion

        #region Fixed Deposits & Recurring Deposits

        public List<FixedDeposit> GetFixedDepositsByCustomerId(int customerId)
        {
            var list = new List<FixedDeposit>();
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT fd.FDId, fd.CustomerId, fd.AccountId, a.AccountNumber, fd.FDNumber, fd.PrincipalAmount, fd.TenureMonths,
                       fd.InterestRate, fd.InterestEarned, fd.MaturityAmount, fd.StartDate, fd.MaturityDate, fd.Status, fd.AutoRenew, fd.CreatedAt
                FROM FixedDeposits fd
                JOIN Accounts a ON fd.AccountId = a.AccountId
                WHERE fd.CustomerId = @CustomerId ORDER BY fd.CreatedAt DESC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FixedDeposit
                {
                    FDId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    AccountId = reader.GetInt32(2),
                    AccountNumber = reader.GetString(3),
                    FDNumber = reader.GetString(4),
                    PrincipalAmount = reader.GetDecimal(5),
                    TenureMonths = reader.GetInt32(6),
                    InterestRate = reader.GetDecimal(7),
                    InterestEarned = reader.GetDecimal(8),
                    MaturityAmount = reader.GetDecimal(9),
                    StartDate = reader.GetDateTime(10),
                    MaturityDate = reader.GetDateTime(11),
                    Status = reader.GetString(12),
                    AutoRenew = reader.GetInt32(13) == 1,
                    CreatedAt = reader.GetDateTime(14)
                });
            }
            return list;
        }

        public (bool Success, string Message, string FDNumber) CreateFixedDeposit(int customerId, int accountId, decimal principal, int tenureMonths, bool autoRenew, bool isSeniorCitizen)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // Verify Balance
                string checkQuery = "SELECT Balance FROM Accounts WHERE AccountId = @AccountId AND CustomerId = @CustomerId AND Status = 'Active'";
                decimal balance;
                using (var checkCmd = new NpgsqlCommand(checkQuery, conn, tx))
                {
                    checkCmd.Parameters.AddWithValue("@AccountId", accountId);
                    checkCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    var res = checkCmd.ExecuteScalar();
                    if (res == null || res == DBNull.Value)
                    {
                        tx.Rollback();
                        return (false, "Source account is inactive or not found.", "");
                    }
                    balance = (decimal)res;
                }

                if (balance < principal)
                {
                    tx.Rollback();
                    return (false, "Insufficient balance in account to open Fixed Deposit.", "");
                }

                // Deduct from account
                string deductQuery = "UPDATE Accounts SET Balance = Balance - @Amount, LastActivityDate = CURRENT_TIMESTAMP WHERE AccountId = @AccountId";
                using (var deductCmd = new NpgsqlCommand(deductQuery, conn, tx))
                {
                    deductCmd.Parameters.AddWithValue("@Amount", principal);
                    deductCmd.Parameters.AddWithValue("@AccountId", accountId);
                    deductCmd.ExecuteNonQuery();
                }

                // Determine Interest Rate
                decimal rate = tenureMonths >= 36 ? 7.65m : (tenureMonths >= 24 ? 7.40m : 7.10m);
                if (isSeniorCitizen) rate += 0.50m;

                // Calculate Compound/Simple Interest for system
                decimal interestEarned = Math.Round(principal * (rate / 100m) * (tenureMonths / 12m), 2);
                decimal maturityAmount = principal + interestEarned;
                string fdNumber = "FD" + DateTime.Now.ToString("yyyyMM") + (1000 + customerId + new Random().Next(10, 99));

                DateTime fdMaturityDate = DateTime.UtcNow.AddMonths(tenureMonths);
                string insertFdQuery = @"
                    INSERT INTO FixedDeposits (CustomerId, AccountId, FDNumber, PrincipalAmount, TenureMonths, InterestRate, InterestEarned, MaturityAmount, StartDate, MaturityDate, Status, AutoRenew, CreatedAt)
                    VALUES (@CustomerId, @AccountId, @FDNumber, @Principal, @Tenure, @Rate, @Interest, @Maturity, CURRENT_TIMESTAMP, @MaturityDate, 'Active', @AutoRenew, CURRENT_TIMESTAMP)";
                using (var fdCmd = new NpgsqlCommand(insertFdQuery, conn, tx))
                {
                    fdCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    fdCmd.Parameters.AddWithValue("@AccountId", accountId);
                    fdCmd.Parameters.AddWithValue("@FDNumber", fdNumber);
                    fdCmd.Parameters.AddWithValue("@Principal", principal);
                    fdCmd.Parameters.AddWithValue("@Tenure", tenureMonths);
                    fdCmd.Parameters.AddWithValue("@Rate", rate);
                    fdCmd.Parameters.AddWithValue("@Interest", interestEarned);
                    fdCmd.Parameters.AddWithValue("@Maturity", maturityAmount);
                    fdCmd.Parameters.AddWithValue("@MaturityDate", fdMaturityDate);
                    fdCmd.Parameters.AddWithValue("@AutoRenew", autoRenew ? 1 : 0);
                    fdCmd.ExecuteNonQuery();
                }

                // Record Debit Transaction
                string txnQuery = @"
                    INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, Status, TransactionDate)
                    VALUES (@TxnRef, @AccountId, 'Deposit', @Amount, @NewBal, 'Debit', 'Investment', @Desc, 'Completed', CURRENT_TIMESTAMP)";
                using (var txnCmd = new NpgsqlCommand(txnQuery, conn, tx))
                {
                    txnCmd.Parameters.AddWithValue("@TxnRef", "TXN" + DateTime.Now.ToString("yyyyMMdd") + (new Random().Next(1000, 9999)));
                    txnCmd.Parameters.AddWithValue("@AccountId", accountId);
                    txnCmd.Parameters.AddWithValue("@Amount", principal);
                    txnCmd.Parameters.AddWithValue("@NewBal", balance - principal);
                    txnCmd.Parameters.AddWithValue("@Desc", "Fixed Deposit Created: " + fdNumber);
                    txnCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return (true, $"Fixed Deposit {fdNumber} created successfully at {rate}% p.a.!", fdNumber);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, "Error creating FD: " + ex.Message, "");
            }
        }

        public List<RecurringDeposit> GetRecurringDepositsByCustomerId(int customerId)
        {
            var list = new List<RecurringDeposit>();
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT rd.RDId, rd.CustomerId, rd.AccountId, a.AccountNumber, rd.RDNumber, rd.MonthlyInstallment, rd.TenureMonths,
                       rd.InterestRate, rd.TotalDeposited, rd.MaturityAmount, rd.StartDate, rd.MaturityDate, rd.InstallmentsPaid, rd.Status, rd.CreatedAt
                FROM RecurringDeposits rd
                JOIN Accounts a ON rd.AccountId = a.AccountId
                WHERE rd.CustomerId = @CustomerId ORDER BY rd.CreatedAt DESC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new RecurringDeposit
                {
                    RDId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    AccountId = reader.GetInt32(2),
                    AccountNumber = reader.GetString(3),
                    RDNumber = reader.GetString(4),
                    MonthlyInstallment = reader.GetDecimal(5),
                    TenureMonths = reader.GetInt32(6),
                    InterestRate = reader.GetDecimal(7),
                    TotalDeposited = reader.GetDecimal(8),
                    MaturityAmount = reader.GetDecimal(9),
                    StartDate = reader.GetDateTime(10),
                    MaturityDate = reader.GetDateTime(11),
                    InstallmentsPaid = reader.GetInt32(12),
                    Status = reader.GetString(13),
                    CreatedAt = reader.GetDateTime(14)
                });
            }
            return list;
        }

        public (bool Success, string Message, string RDNumber) CreateRecurringDeposit(int customerId, int accountId, decimal installment, int tenureMonths)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // Check Balance for first installment
                string checkQuery = "SELECT Balance FROM Accounts WHERE AccountId = @AccountId AND CustomerId = @CustomerId AND Status = 'Active'";
                decimal balance;
                using (var checkCmd = new NpgsqlCommand(checkQuery, conn, tx))
                {
                    checkCmd.Parameters.AddWithValue("@AccountId", accountId);
                    checkCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    var res = checkCmd.ExecuteScalar();
                    if (res == null || res == DBNull.Value)
                    {
                        tx.Rollback();
                        return (false, "Source account is inactive.", "");
                    }
                    balance = (decimal)res;
                }

                if (balance < installment)
                {
                    tx.Rollback();
                    return (false, "Insufficient balance for 1st monthly installment.", "");
                }

                // Deduct 1st installment
                string deductQuery = "UPDATE Accounts SET Balance = Balance - @Amount, LastActivityDate = CURRENT_TIMESTAMP WHERE AccountId = @AccountId";
                using (var deductCmd = new NpgsqlCommand(deductQuery, conn, tx))
                {
                    deductCmd.Parameters.AddWithValue("@Amount", installment);
                    deductCmd.Parameters.AddWithValue("@AccountId", accountId);
                    deductCmd.ExecuteNonQuery();
                }

                decimal rate = 7.00m;
                decimal totalInvested = installment * tenureMonths;
                decimal estimatedInterest = Math.Round((installment * tenureMonths * (tenureMonths + 1) / 24m) * (rate / 100m), 2);
                decimal maturityAmount = totalInvested + estimatedInterest;
                string rdNumber = "RD" + DateTime.Now.ToString("yyyyMM") + (2000 + customerId + new Random().Next(10, 99));

                DateTime rdMaturityDate = DateTime.UtcNow.AddMonths(tenureMonths);
                string insertRdQuery = @"
                    INSERT INTO RecurringDeposits (CustomerId, AccountId, RDNumber, MonthlyInstallment, TenureMonths, InterestRate, TotalDeposited, MaturityAmount, StartDate, MaturityDate, InstallmentsPaid, Status, CreatedAt)
                    VALUES (@CustomerId, @AccountId, @RDNumber, @Installment, @Tenure, @Rate, @Installment, @Maturity, CURRENT_TIMESTAMP, @MaturityDate, 1, 'Active', CURRENT_TIMESTAMP)";
                using (var rdCmd = new NpgsqlCommand(insertRdQuery, conn, tx))
                {
                    rdCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    rdCmd.Parameters.AddWithValue("@AccountId", accountId);
                    rdCmd.Parameters.AddWithValue("@RDNumber", rdNumber);
                    rdCmd.Parameters.AddWithValue("@Installment", installment);
                    rdCmd.Parameters.AddWithValue("@Tenure", tenureMonths);
                    rdCmd.Parameters.AddWithValue("@Rate", rate);
                    rdCmd.Parameters.AddWithValue("@Maturity", maturityAmount);
                    rdCmd.Parameters.AddWithValue("@MaturityDate", rdMaturityDate);
                    rdCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return (true, $"Recurring Deposit {rdNumber} started successfully!", rdNumber);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, "Error creating RD: " + ex.Message, "");
            }
        }

        #endregion

        #region Loans Management

        public List<Loan> GetLoansByCustomerId(int customerId)
        {
            var list = new List<Loan>();
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT l.LoanId, l.CustomerId, u.FullName, u.Email, l.LoanType, l.LoanAmount, l.TenureMonths,
                       l.InterestRate, l.MonthlyEMI, l.TotalRepayment, l.MonthlyIncome, l.EmploymentType,
                       l.Purpose, l.CreditScore, l.Status, l.Remarks, l.ApprovedBy, l.ApprovedDate, l.DisbursedDate,
                       l.OutstandingBalance, l.NextEMIDate, l.CreatedAt
                FROM Loans l
                JOIN Customers c ON l.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE l.CustomerId = @CustomerId ORDER BY l.CreatedAt DESC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Loan
                {
                    LoanId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    CustomerName = reader.GetString(2),
                    CustomerEmail = reader.GetString(3),
                    LoanType = reader.GetString(4),
                    LoanAmount = reader.GetDecimal(5),
                    TenureMonths = reader.GetInt32(6),
                    InterestRate = reader.GetDecimal(7),
                    MonthlyEMI = reader.GetDecimal(8),
                    TotalRepayment = reader.GetDecimal(9),
                    MonthlyIncome = reader.GetDecimal(10),
                    EmploymentType = reader.GetString(11),
                    Purpose = reader.GetString(12),
                    CreditScore = reader.GetInt32(13),
                    Status = reader.GetString(14),
                    Remarks = reader.IsDBNull(15) ? null : reader.GetString(15),
                    ApprovedBy = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                    ApprovedDate = reader.IsDBNull(17) ? null : reader.GetDateTime(17),
                    DisbursedDate = reader.IsDBNull(18) ? null : reader.GetDateTime(18),
                    OutstandingBalance = reader.GetDecimal(19),
                    NextEMIDate = reader.IsDBNull(20) ? null : reader.GetDateTime(20),
                    CreatedAt = reader.GetDateTime(21)
                });
            }
            return list;
        }

        public (bool Success, string Message, int LoanId) ApplyForLoan(int customerId, LoanApplyViewModel model)
        {
            using var conn = GetConnection();
            conn.Open();

            // Calculate EMI formula: P * r * (1+r)^n / ((1+r)^n - 1)
            double p = (double)model.LoanAmount;
            double r = (double)model.InterestRate / 1200.0;
            int n = model.TenureMonths;
            double emi = (p * r * Math.Pow(1 + r, n)) / (Math.Pow(1 + r, n) - 1);
            decimal monthlyEmi = Math.Round((decimal)emi, 2);
            decimal totalRepay = monthlyEmi * n;

            string query = @"
                INSERT INTO Loans (CustomerId, LoanType, LoanAmount, TenureMonths, InterestRate, MonthlyEMI, TotalRepayment, MonthlyIncome, EmploymentType, Purpose, CreditScore, Status, OutstandingBalance, CreatedAt)
                VALUES (@CustomerId, @LoanType, @Amount, @Tenure, @Rate, @EMI, @TotalRepay, @Income, @EmpType, @Purpose, @Score, 'Pending', @Amount, CURRENT_TIMESTAMP)
                RETURNING LoanId";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.Parameters.AddWithValue("@LoanType", model.LoanType);
            cmd.Parameters.AddWithValue("@Amount", model.LoanAmount);
            cmd.Parameters.AddWithValue("@Tenure", model.TenureMonths);
            cmd.Parameters.AddWithValue("@Rate", model.InterestRate);
            cmd.Parameters.AddWithValue("@EMI", monthlyEmi);
            cmd.Parameters.AddWithValue("@TotalRepay", totalRepay);
            cmd.Parameters.AddWithValue("@Income", model.MonthlyIncome);
            cmd.Parameters.AddWithValue("@EmpType", model.EmploymentType);
            cmd.Parameters.AddWithValue("@Purpose", model.Purpose.Trim());
            cmd.Parameters.AddWithValue("@Score", model.SimulatedCreditScore);

            int loanId = Convert.ToInt32(cmd.ExecuteScalar());

            // Log notification
            string notifQuery = @"
                INSERT INTO Notifications (UserId, Title, Message, NotificationType)
                SELECT UserId, 'Loan Application Submitted', 'Your application for " + model.LoanType + " of INR " + model.LoanAmount.ToString("N0") + " is submitted for review.', 'General' FROM Customers WHERE CustomerId = @CustomerId";
            using var notifCmd = new NpgsqlCommand(notifQuery, conn);
            notifCmd.Parameters.AddWithValue("@CustomerId", customerId);
            notifCmd.ExecuteNonQuery();

            return (true, "Loan application submitted successfully! Your application reference is LOAN-" + loanId, loanId);
        }

        #endregion

        #region Debit & Credit Cards Management

        public List<DebitCard> GetDebitCardsByCustomerId(int customerId)
        {
            var list = new List<DebitCard>();
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT dc.DebitCardId, dc.CustomerId, dc.AccountId, a.AccountNumber, dc.CardNumber, dc.CardHolderName,
                       dc.CardType, dc.ExpiryDate, dc.CVV, dc.PINHash, dc.DailyLimit, dc.IsInternationalEnabled,
                       dc.IsOnlineEnabled, dc.IsContactlessEnabled, dc.Status, dc.IssuedDate
                FROM DebitCards dc
                JOIN Accounts a ON dc.AccountId = a.AccountId
                WHERE dc.CustomerId = @CustomerId";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DebitCard
                {
                    DebitCardId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    AccountId = reader.GetInt32(2),
                    AccountNumber = reader.GetString(3),
                    CardNumber = reader.GetString(4),
                    CardHolderName = reader.GetString(5),
                    CardType = reader.GetString(6),
                    ExpiryDate = reader.GetString(7),
                    CVV = reader.GetString(8),
                    PINHash = reader.GetString(9),
                    DailyLimit = reader.GetDecimal(10),
                    IsInternationalEnabled = reader.GetInt32(11) == 1,
                    IsOnlineEnabled = reader.GetInt32(12) == 1,
                    IsContactlessEnabled = reader.GetInt32(13) == 1,
                    Status = reader.GetString(14),
                    IssuedDate = reader.GetDateTime(15)
                });
            }
            return list;
        }

        public List<CreditCard> GetCreditCardsByCustomerId(int customerId)
        {
            var list = new List<CreditCard>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT CreditCardId, CustomerId, CardNumber, CardHolderName, CardType, CreditLimit, AvailableLimit, ExpiryDate, CVV, PINHash, RewardPoints, IsInternationalEnabled, Status, BillingCycleDay, IssuedDate FROM CreditCards WHERE CustomerId = @CustomerId";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CreditCard
                {
                    CreditCardId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    CardNumber = reader.GetString(2),
                    CardHolderName = reader.GetString(3),
                    CardType = reader.GetString(4),
                    CreditLimit = reader.GetDecimal(5),
                    AvailableLimit = reader.GetDecimal(6),
                    ExpiryDate = reader.GetString(7),
                    CVV = reader.GetString(8),
                    PINHash = reader.GetString(9),
                    RewardPoints = reader.GetInt32(10),
                    IsInternationalEnabled = reader.GetInt32(11) == 1,
                    Status = reader.GetString(12),
                    BillingCycleDay = reader.GetInt32(13),
                    IssuedDate = reader.GetDateTime(14)
                });
            }
            return list;
        }

        public bool ToggleCardStatus(int cardId, string cardCategory, string newStatus, int customerId)
        {
            using var conn = GetConnection();
            conn.Open();
            string table = cardCategory == "Credit" ? "CreditCards" : "DebitCards";
            string idCol = cardCategory == "Credit" ? "CreditCardId" : "DebitCardId";
            string query = $"UPDATE {table} SET Status = @Status WHERE {idCol} = @CardId AND CustomerId = @CustomerId";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Status", newStatus);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateCardPIN(int cardId, string cardCategory, string newPin, int customerId)
        {
            using var conn = GetConnection();
            conn.Open();
            string table = cardCategory == "Credit" ? "CreditCards" : "DebitCards";
            string idCol = cardCategory == "Credit" ? "CreditCardId" : "DebitCardId";
            string query = $"UPDATE {table} SET PINHash = @PIN WHERE {idCol} = @CardId AND CustomerId = @CustomerId";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PIN", newPin);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateCardLimit(int cardId, string cardCategory, decimal newLimit, int customerId)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = cardCategory == "Credit" 
                ? "UPDATE CreditCards SET CreditLimit = @Limit WHERE CreditCardId = @CardId AND CustomerId = @CustomerId"
                : "UPDATE DebitCards SET DailyLimit = @Limit WHERE DebitCardId = @CardId AND CustomerId = @CustomerId";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Limit", newLimit);
            cmd.Parameters.AddWithValue("@CardId", cardId);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            return cmd.ExecuteNonQuery() > 0;
        }

        #endregion

        #region Bill Payments & Recharge

        public (bool Success, string Message, string Reference) ProcessBillPayment(BillPayViewModel model, int customerId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // Verify Account Balance
                string checkQuery = "SELECT Balance FROM Accounts WHERE AccountId = @AccountId AND CustomerId = @CustomerId AND Status = 'Active'";
                decimal balance;
                using (var checkCmd = new NpgsqlCommand(checkQuery, conn, tx))
                {
                    checkCmd.Parameters.AddWithValue("@AccountId", model.AccountId);
                    checkCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    var res = checkCmd.ExecuteScalar();
                    if (res == null || res == DBNull.Value)
                    {
                        tx.Rollback();
                        return (false, "Selected account is not active.", "");
                    }
                    balance = (decimal)res;
                }

                if (balance < model.Amount)
                {
                    tx.Rollback();
                    return (false, "Insufficient balance for bill payment.", "");
                }

                // Deduct from account
                string deductQuery = "UPDATE Accounts SET Balance = Balance - @Amount, LastActivityDate = CURRENT_TIMESTAMP WHERE AccountId = @AccountId";
                using (var deductCmd = new NpgsqlCommand(deductQuery, conn, tx))
                {
                    deductCmd.Parameters.AddWithValue("@Amount", model.Amount);
                    deductCmd.Parameters.AddWithValue("@AccountId", model.AccountId);
                    deductCmd.ExecuteNonQuery();
                }

                string txnRef = "BIL" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999);

                // Insert into BillPayments
                string billQuery = @"
                    INSERT INTO BillPayments (CustomerId, AccountId, BillerCategory, BillerName, ConsumerNumber, Amount, TransactionReference, Status, PaymentDate)
                    VALUES (@CustomerId, @AccountId, @Cat, @Name, @Consumer, @Amount, @TxnRef, 'Success', CURRENT_TIMESTAMP)";
                using (var billCmd = new NpgsqlCommand(billQuery, conn, tx))
                {
                    billCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    billCmd.Parameters.AddWithValue("@AccountId", model.AccountId);
                    billCmd.Parameters.AddWithValue("@Cat", model.BillerCategory);
                    billCmd.Parameters.AddWithValue("@Name", model.BillerName.Trim());
                    billCmd.Parameters.AddWithValue("@Consumer", model.ConsumerNumber.Trim());
                    billCmd.Parameters.AddWithValue("@Amount", model.Amount);
                    billCmd.Parameters.AddWithValue("@TxnRef", txnRef);
                    billCmd.ExecuteNonQuery();
                }

                // Record Transaction
                string txnQuery = @"
                    INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status, TransactionDate)
                    VALUES (@TxnRef, @AccountId, 'BillPayment', @Amount, @NewBal, 'Debit', 'Utilities', @Desc, @Consumer, @Biller, 'Online', 'Completed', CURRENT_TIMESTAMP)";
                using (var txnCmd = new NpgsqlCommand(txnQuery, conn, tx))
                {
                    txnCmd.Parameters.AddWithValue("@TxnRef", txnRef);
                    txnCmd.Parameters.AddWithValue("@AccountId", model.AccountId);
                    txnCmd.Parameters.AddWithValue("@Amount", model.Amount);
                    txnCmd.Parameters.AddWithValue("@NewBal", balance - model.Amount);
                    txnCmd.Parameters.AddWithValue("@Desc", $"Bill Payment: {model.BillerCategory} - {model.BillerName}");
                    txnCmd.Parameters.AddWithValue("@Consumer", model.ConsumerNumber.Trim());
                    txnCmd.Parameters.AddWithValue("@Biller", model.BillerName.Trim());
                    txnCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return (true, $"Bill payment of INR {model.Amount:N2} to {model.BillerName} successful!", txnRef);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, "Bill payment error: " + ex.Message, "");
            }
        }

        public List<BillPayment> GetBillPaymentsByCustomerId(int customerId)
        {
            var list = new List<BillPayment>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT BillPaymentId, CustomerId, AccountId, BillerCategory, BillerName, ConsumerNumber, Amount, TransactionReference, Status, PaymentDate FROM BillPayments WHERE CustomerId = @CustomerId ORDER BY PaymentDate DESC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BillPayment
                {
                    BillPaymentId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    AccountId = reader.GetInt32(2),
                    BillerCategory = reader.GetString(3),
                    BillerName = reader.GetString(4),
                    ConsumerNumber = reader.GetString(5),
                    Amount = reader.GetDecimal(6),
                    TransactionReference = reader.GetString(7),
                    Status = reader.GetString(8),
                    PaymentDate = reader.GetDateTime(9)
                });
            }
            return list;
        }

        #endregion

        #region Investments & Portfolio

        public List<Investment> GetInvestmentsByCustomerId(int customerId)
        {
            var list = new List<Investment>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT InvestmentId, CustomerId, AccountId, InvestmentType, SchemeName, InvestedAmount, CurrentValue, Units, NAV, ReturnsPercent, Status, InvestmentDate FROM Investments WHERE CustomerId = @CustomerId ORDER BY InvestmentDate DESC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Investment
                {
                    InvestmentId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    AccountId = reader.GetInt32(2),
                    InvestmentType = reader.GetString(3),
                    SchemeName = reader.GetString(4),
                    InvestedAmount = reader.GetDecimal(5),
                    CurrentValue = reader.GetDecimal(6),
                    Units = reader.GetDecimal(7),
                    NAV = reader.GetDecimal(8),
                    ReturnsPercent = reader.GetDecimal(9),
                    Status = reader.GetString(10),
                    InvestmentDate = reader.GetDateTime(11)
                });
            }
            return list;
        }

        public bool AddInvestment(int customerId, int accountId, string type, string schemeName, decimal amount)
        {
            using var conn = GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // Check balance
                string checkQuery = "SELECT Balance FROM Accounts WHERE AccountId = @AccountId AND CustomerId = @CustomerId AND Status = 'Active'";
                decimal balance;
                using (var checkCmd = new NpgsqlCommand(checkQuery, conn, tx))
                {
                    checkCmd.Parameters.AddWithValue("@AccountId", accountId);
                    checkCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    var res = checkCmd.ExecuteScalar();
                    if (res == null || res == DBNull.Value) { tx.Rollback(); return false; }
                    balance = (decimal)res;
                }

                if (balance < amount) { tx.Rollback(); return false; }

                // Deduct
                string deductQuery = "UPDATE Accounts SET Balance = Balance - @Amount WHERE AccountId = @AccountId";
                using (var deductCmd = new NpgsqlCommand(deductQuery, conn, tx))
                {
                    deductCmd.Parameters.AddWithValue("@Amount", amount);
                    deductCmd.Parameters.AddWithValue("@AccountId", accountId);
                    deductCmd.ExecuteNonQuery();
                }

                // Insert Investment
                decimal nav = type == "Gold Investment" ? 6800.00m : 105.50m;
                decimal units = Math.Round(amount / nav, 4);
                string insertQuery = @"
                    INSERT INTO Investments (CustomerId, AccountId, InvestmentType, SchemeName, InvestedAmount, CurrentValue, Units, NAV, ReturnsPercent, Status, InvestmentDate)
                    VALUES (@CustomerId, @AccountId, @Type, @Scheme, @Amount, @Amount, @Units, @NAV, 0.0, 'Active', CURRENT_TIMESTAMP)";
                using (var insCmd = new NpgsqlCommand(insertQuery, conn, tx))
                {
                    insCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    insCmd.Parameters.AddWithValue("@AccountId", accountId);
                    insCmd.Parameters.AddWithValue("@Type", type);
                    insCmd.Parameters.AddWithValue("@Scheme", schemeName);
                    insCmd.Parameters.AddWithValue("@Amount", amount);
                    insCmd.Parameters.AddWithValue("@Units", units);
                    insCmd.Parameters.AddWithValue("@NAV", nav);
                    insCmd.ExecuteNonQuery();
                }

                // Transaction
                string txnQuery = @"
                    INSERT INTO Transactions (TransactionReference, AccountId, TransactionType, Amount, BalanceAfter, DebitOrCredit, Category, Description, Status, TransactionDate)
                    VALUES (@TxnRef, @AccountId, 'Investment', @Amount, @NewBal, 'Debit', 'Investment', @Desc, 'Completed', CURRENT_TIMESTAMP)";
                using (var txnCmd = new NpgsqlCommand(txnQuery, conn, tx))
                {
                    txnCmd.Parameters.AddWithValue("@TxnRef", "INV" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999));
                    txnCmd.Parameters.AddWithValue("@AccountId", accountId);
                    txnCmd.Parameters.AddWithValue("@Amount", amount);
                    txnCmd.Parameters.AddWithValue("@NewBal", balance - amount);
                    txnCmd.Parameters.AddWithValue("@Desc", "Investment in " + schemeName);
                    txnCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                return false;
            }
        }

        #endregion

        #region Service Requests & Cheque Books

        public List<ChequeRequest> GetChequeRequestsByCustomerId(int customerId)
        {
            var list = new List<ChequeRequest>();
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT cr.RequestId, cr.CustomerId, u.FullName, cr.AccountId, a.AccountNumber, cr.NumberOfLeaves,
                       cr.DeliveryAddress, cr.Status, cr.Remarks, cr.ProcessedBy, cr.RequestDate, cr.ProcessedDate
                FROM ChequeRequests cr
                JOIN Accounts a ON cr.AccountId = a.AccountId
                JOIN Customers c ON cr.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE cr.CustomerId = @CustomerId ORDER BY cr.RequestDate DESC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ChequeRequest
                {
                    RequestId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    CustomerName = reader.GetString(2),
                    AccountId = reader.GetInt32(3),
                    AccountNumber = reader.GetString(4),
                    NumberOfLeaves = reader.GetInt32(5),
                    DeliveryAddress = reader.GetString(6),
                    Status = reader.GetString(7),
                    Remarks = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ProcessedBy = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    RequestDate = reader.GetDateTime(10),
                    ProcessedDate = reader.IsDBNull(11) ? null : reader.GetDateTime(11)
                });
            }
            return list;
        }

        public bool RequestChequeBook(int customerId, int accountId, int leaves, string address)
        {
            if (!AccountBelongsToCustomer(accountId, customerId)) return false;
            using var conn = GetConnection();
            conn.Open();
            string query = "INSERT INTO ChequeRequests (CustomerId, AccountId, NumberOfLeaves, DeliveryAddress, Status, RequestDate) VALUES (@CustomerId, @AccountId, @Leaves, @Address, 'Pending', CURRENT_TIMESTAMP)";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.Parameters.AddWithValue("@AccountId", accountId);
            cmd.Parameters.AddWithValue("@Leaves", leaves);
            cmd.Parameters.AddWithValue("@Address", address.Trim());
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<ServiceRequest> GetServiceRequestsByCustomerId(int customerId)
        {
            var list = new List<ServiceRequest>();
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT sr.ServiceRequestId, sr.CustomerId, u.FullName, sr.RequestType, sr.OldValue, sr.NewValue, sr.Reason, sr.Status, sr.Remarks, sr.ProcessedBy, sr.RequestDate, sr.ProcessedDate
                FROM ServiceRequests sr
                JOIN Customers c ON sr.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE sr.CustomerId = @CustomerId ORDER BY sr.RequestDate DESC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ServiceRequest
                {
                    ServiceRequestId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    CustomerName = reader.GetString(2),
                    RequestType = reader.GetString(3),
                    OldValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                    NewValue = reader.GetString(5),
                    Reason = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Status = reader.GetString(7),
                    Remarks = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ProcessedBy = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    RequestDate = reader.GetDateTime(10),
                    ProcessedDate = reader.IsDBNull(11) ? null : reader.GetDateTime(11)
                });
            }
            return list;
        }

        public bool CreateServiceRequest(int customerId, string requestType, string? oldValue, string newValue, string? reason)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = "INSERT INTO ServiceRequests (CustomerId, RequestType, OldValue, NewValue, Reason, Status, RequestDate) VALUES (@CustomerId, @Type, @Old, @New, @Reason, 'Pending', CURRENT_TIMESTAMP)";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.Parameters.AddWithValue("@Type", requestType);
            cmd.Parameters.AddWithValue("@Old", (object?)oldValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@New", newValue.Trim());
            cmd.Parameters.AddWithValue("@Reason", (object?)reason ?? DBNull.Value);
            return cmd.ExecuteNonQuery() > 0;
        }

        #endregion

        #region Support Tickets & Chat

        public List<SupportTicket> GetSupportTicketsByCustomerId(int customerId)
        {
            var list = new List<SupportTicket>();
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT t.TicketId, t.CustomerId, u.FullName, t.TicketNumber, t.Subject, t.Category, t.Message, t.Priority, t.Status, t.AssignedEmployeeId, t.Resolution, t.CreatedAt, t.ResolvedAt
                FROM SupportTickets t
                JOIN Customers c ON t.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE t.CustomerId = @CustomerId ORDER BY t.CreatedAt DESC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SupportTicket
                {
                    TicketId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    CustomerName = reader.GetString(2),
                    TicketNumber = reader.GetString(3),
                    Subject = reader.GetString(4),
                    Category = reader.GetString(5),
                    Message = reader.GetString(6),
                    Priority = reader.GetString(7),
                    Status = reader.GetString(8),
                    AssignedEmployeeId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    Resolution = reader.IsDBNull(10) ? null : reader.GetString(10),
                    CreatedAt = reader.GetDateTime(11),
                    ResolvedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12)
                });
            }
            return list;
        }

        public string CreateSupportTicket(int customerId, string subject, string category, string message, string priority = "Normal")
        {
            using var conn = GetConnection();
            conn.Open();
            string ticketNumber = "TCK" + DateTime.Now.ToString("yyyy") + new Random().Next(1000, 9999);
            string query = @"
                INSERT INTO SupportTickets (CustomerId, TicketNumber, Subject, Category, Message, Priority, Status, CreatedAt)
                VALUES (@CustomerId, @TicketNumber, @Subject, @Category, @Message, @Priority, 'Open', CURRENT_TIMESTAMP)";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.Parameters.AddWithValue("@TicketNumber", ticketNumber);
            cmd.Parameters.AddWithValue("@Subject", subject.Trim());
            cmd.Parameters.AddWithValue("@Category", category);
            cmd.Parameters.AddWithValue("@Message", message.Trim());
            cmd.Parameters.AddWithValue("@Priority", priority);
            cmd.ExecuteNonQuery();
            return ticketNumber;
        }

        public List<ChatMessage> GetChatMessages(int customerId)
        {
            var list = new List<ChatMessage>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT MessageId, CustomerId, Sender, SenderName, Message, IsRead, SentAt FROM ChatMessages WHERE CustomerId = @CustomerId ORDER BY SentAt ASC";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ChatMessage
                {
                    MessageId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    Sender = reader.GetString(2),
                    SenderName = reader.GetString(3),
                    Message = reader.GetString(4),
                    IsRead = reader.GetInt32(5) == 1,
                    SentAt = reader.GetDateTime(6)
                });
            }
            return list;
        }

        public void SaveChatMessage(int customerId, string sender, string senderName, string message)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = "INSERT INTO ChatMessages (CustomerId, Sender, SenderName, Message, IsRead, SentAt) VALUES (@CustomerId, @Sender, @SenderName, @Message, 0, CURRENT_TIMESTAMP)";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CustomerId", customerId);
            cmd.Parameters.AddWithValue("@Sender", sender);
            cmd.Parameters.AddWithValue("@SenderName", senderName);
            cmd.Parameters.AddWithValue("@Message", message.Trim());
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Notifications & Security

        public List<Notification> GetNotificationsByUserId(int userId, int limit = 20)
        {
            var list = new List<Notification>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT  NotificationId, UserId, Title, Message, NotificationType, IsRead, CreatedAt FROM Notifications WHERE UserId = @UserId  ORDER BY CreatedAt DESC LIMIT @Limit";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Limit", limit);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Notification
                {
                    NotificationId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    Title = reader.GetString(2),
                    Message = reader.GetString(3),
                    NotificationType = reader.GetString(4),
                    IsRead = reader.GetInt32(5) == 1,
                    CreatedAt = reader.GetDateTime(6)
                });
            }
            return list;
        }

        public void MarkNotificationAsRead(int notificationId, int userId)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = "UPDATE Notifications SET IsRead = 1 WHERE NotificationId = @Id AND UserId = @UserId";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", notificationId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Dynamic Content & Admin Controls

        public List<Advertisement> GetActiveAdvertisements()
        {
            var list = new List<Advertisement>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT AdId, Title, Subtitle, TagText, BadgeColor, CallToActionText, TargetUrl, ImageUrl, DisplayOrder, IsActive, StartDate, EndDate FROM Advertisements WHERE IsActive = 1 AND StartDate <= CURRENT_TIMESTAMP AND EndDate >= CURRENT_TIMESTAMP ORDER BY DisplayOrder ASC";
            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Advertisement
                {
                    AdId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Subtitle = reader.GetString(2),
                    TagText = reader.GetString(3),
                    BadgeColor = reader.GetString(4),
                    CallToActionText = reader.GetString(5),
                    TargetUrl = reader.GetString(6),
                    ImageUrl = reader.GetString(7),
                    DisplayOrder = reader.GetInt32(8),
                    IsActive = reader.GetInt32(9) == 1,
                    StartDate = reader.GetDateTime(10),
                    EndDate = reader.GetDateTime(11)
                });
            }
            return list;
        }

        public List<FAQ> GetActiveFAQs(string? category = null)
        {
            var list = new List<FAQ>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT FAQId, Category, Question, Answer, DisplayOrder, IsActive FROM FAQs WHERE IsActive = 1 " +
                (!string.IsNullOrEmpty(category) ? "AND Category = @Category " : "") +
                "ORDER BY DisplayOrder ASC";
            using var cmd = new NpgsqlCommand(query, conn);
            if (!string.IsNullOrEmpty(category)) cmd.Parameters.AddWithValue("@Category", category);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FAQ
                {
                    FAQId = reader.GetInt32(0),
                    Category = reader.GetString(1),
                    Question = reader.GetString(2),
                    Answer = reader.GetString(3),
                    DisplayOrder = reader.GetInt32(4),
                    IsActive = reader.GetInt32(5) == 1
                });
            }
            return list;
        }

        public List<Offer> GetActiveOffers()
        {
            var list = new List<Offer>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT OfferId, Title, Description, Category, DiscountCode, ValidTill, IsActive FROM Offers WHERE IsActive = 1 AND ValidTill >= CURRENT_TIMESTAMP ORDER BY ValidTill ASC";
            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Offer
                {
                    OfferId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    Category = reader.GetString(3),
                    DiscountCode = reader.IsDBNull(4) ? null : reader.GetString(4),
                    ValidTill = reader.GetDateTime(5),
                    IsActive = reader.GetInt32(6) == 1
                });
            }
            return list;
        }

        public List<CurrencyRate> GetCurrencyRates()
        {
            var list = new List<CurrencyRate>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT CurrencyId, CurrencyCode, CurrencyName, BuyRate, SellRate, LastUpdated FROM CurrencyRates ORDER BY CurrencyCode";
            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CurrencyRate
                {
                    CurrencyId = reader.GetInt32(0),
                    CurrencyCode = reader.GetString(1),
                    CurrencyName = reader.GetString(2),
                    BuyRate = reader.GetDecimal(3),
                    SellRate = reader.GetDecimal(4),
                    LastUpdated = reader.GetDateTime(5)
                });
            }
            return list;
        }

        public List<InterestRate> GetInterestRates()
        {
            var list = new List<InterestRate>();
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT RateId, ProductType, TenureOrTier, RegularRate, SeniorCitizenRate, LastUpdated FROM InterestRates ORDER BY ProductType, RateId";
            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new InterestRate
                {
                    RateId = reader.GetInt32(0),
                    ProductType = reader.GetString(1),
                    TenureOrTier = reader.GetString(2),
                    RegularRate = reader.GetDecimal(3),
                    SeniorCitizenRate = reader.GetDecimal(4),
                    LastUpdated = reader.GetDateTime(5)
                });
            }
            return list;
        }

        #endregion

        #region Employee & Manager Portals Data Access

        public EmployeeDashboardViewModel GetEmployeeDashboard()
        {
            var vm = new EmployeeDashboardViewModel();
            using var conn = GetConnection();
            conn.Open();

            // Pending KYC Customers
            string kycQuery = @"
                SELECT  c.CustomerId, c.UserId, c.CustomerCode, c.DateOfBirth, c.Gender, c.Address, c.City, c.State, c.Pincode,
                              c.PAN, c.Aadhaar, c.KYCStatus, c.KYCRemarks, c.NomineeName, c.NomineeRelation, c.Segment, c.CreatedAt,
                              u.FullName, u.Email, u.MobileNumber, u.Username
                FROM Customers c JOIN Users u ON c.UserId = u.UserId
                WHERE c.KYCStatus = 'Pending'  ORDER BY c.CreatedAt ASC LIMIT 10";
            using (var cmd = new NpgsqlCommand(kycQuery, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    vm.PendingKYCCustomers.Add(new Customer
                    {
                        CustomerId = r.GetInt32(0),
                        UserId = r.GetInt32(1),
                        CustomerCode = r.GetString(2),
                        DateOfBirth = r.GetDateTime(3),
                        Gender = r.GetString(4),
                        Address = r.GetString(5),
                        City = r.GetString(6),
                        State = r.GetString(7),
                        Pincode = r.GetString(8),
                        PAN = r.GetString(9),
                        Aadhaar = r.GetString(10),
                        KYCStatus = r.GetString(11),
                        FullName = r.GetString(17),
                        Email = r.GetString(18),
                        MobileNumber = r.GetString(19),
                        Username = r.GetString(20)
                    });
                }
            }

            // Pending Cheque Requests
            string chqQuery = @"
                SELECT  cr.RequestId, cr.CustomerId, u.FullName, cr.AccountId, a.AccountNumber, cr.NumberOfLeaves, cr.DeliveryAddress, cr.Status, cr.Remarks, cr.ProcessedBy, cr.RequestDate, cr.ProcessedDate
                FROM ChequeRequests cr
                JOIN Customers c ON cr.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                JOIN Accounts a ON cr.AccountId = a.AccountId
                WHERE cr.Status = 'Pending'  ORDER BY cr.RequestDate ASC LIMIT 10";
            using (var cmd = new NpgsqlCommand(chqQuery, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    vm.PendingChequeRequests.Add(new ChequeRequest
                    {
                        RequestId = r.GetInt32(0),
                        CustomerId = r.GetInt32(1),
                        CustomerName = r.GetString(2),
                        AccountId = r.GetInt32(3),
                        AccountNumber = r.GetString(4),
                        NumberOfLeaves = r.GetInt32(5),
                        DeliveryAddress = r.GetString(6),
                        Status = r.GetString(7),
                        RequestDate = r.GetDateTime(10)
                    });
                }
            }

            // Pending Service Requests
            string srvQuery = @"
                SELECT  sr.ServiceRequestId, sr.CustomerId, u.FullName, sr.RequestType, sr.OldValue, sr.NewValue, sr.Reason, sr.Status, sr.Remarks, sr.ProcessedBy, sr.RequestDate, sr.ProcessedDate
                FROM ServiceRequests sr
                JOIN Customers c ON sr.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE sr.Status = 'Pending'  ORDER BY sr.RequestDate ASC LIMIT 10";
            using (var cmd = new NpgsqlCommand(srvQuery, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    vm.PendingServiceRequests.Add(new ServiceRequest
                    {
                        ServiceRequestId = r.GetInt32(0),
                        CustomerId = r.GetInt32(1),
                        CustomerName = r.GetString(2),
                        RequestType = r.GetString(3),
                        OldValue = r.IsDBNull(4) ? null : r.GetString(4),
                        NewValue = r.GetString(5),
                        Reason = r.IsDBNull(6) ? null : r.GetString(6),
                        Status = r.GetString(7),
                        RequestDate = r.GetDateTime(10)
                    });
                }
            }

            // Open Support Tickets
            string tckQuery = @"
                SELECT  t.TicketId, t.CustomerId, u.FullName, t.TicketNumber, t.Subject, t.Category, t.Message, t.Priority, t.Status, t.AssignedEmployeeId, t.Resolution, t.CreatedAt, t.ResolvedAt
                FROM SupportTickets t
                JOIN Customers c ON t.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE t.Status IN ('Open', 'In Progress')  ORDER BY t.CreatedAt ASC LIMIT 10";
            using (var cmd = new NpgsqlCommand(tckQuery, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    vm.AssignedTickets.Add(new SupportTicket
                    {
                        TicketId = r.GetInt32(0),
                        CustomerId = r.GetInt32(1),
                        CustomerName = r.GetString(2),
                        TicketNumber = r.GetString(3),
                        Subject = r.GetString(4),
                        Category = r.GetString(5),
                        Message = r.GetString(6),
                        Priority = r.GetString(7),
                        Status = r.GetString(8),
                        CreatedAt = r.GetDateTime(11)
                    });
                }
            }

            vm.PendingKYCVerificationCount = vm.PendingKYCCustomers.Count;
            vm.PendingChequeRequestsCount = vm.PendingChequeRequests.Count;
            vm.PendingServiceRequestsCount = vm.PendingServiceRequests.Count;
            vm.OpenCustomerTicketsCount = vm.AssignedTickets.Count;

            return vm;
        }

        public ManagerDashboardViewModel GetManagerDashboard()
        {
            var vm = new ManagerDashboardViewModel();
            using var conn = GetConnection();
            conn.Open();

            // Counts & Volumes
            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM Customers", conn))
                vm.TotalBranchCustomers = Convert.ToInt32(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM Accounts", conn))
                vm.TotalBranchAccounts = Convert.ToInt32(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(Balance), 0) FROM Accounts", conn))
                vm.TotalBranchDeposits = Convert.ToDecimal(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(LoanAmount), 0) FROM Loans WHERE Status = 'Active'", conn))
                vm.TotalBranchLoans = Convert.ToDecimal(cmd.ExecuteScalar());

            // Pending Loans
            string loanQuery = @"
                SELECT l.LoanId, l.CustomerId, u.FullName, u.Email, l.LoanType, l.LoanAmount, l.TenureMonths,
                       l.InterestRate, l.MonthlyEMI, l.TotalRepayment, l.MonthlyIncome, l.EmploymentType,
                       l.Purpose, l.CreditScore, l.Status, l.Remarks, l.ApprovedBy, l.ApprovedDate, l.DisbursedDate,
                       l.OutstandingBalance, l.NextEMIDate, l.CreatedAt
                FROM Loans l
                JOIN Customers c ON l.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE l.Status = 'Pending' ORDER BY l.CreatedAt ASC";
            using (var cmd = new NpgsqlCommand(loanQuery, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    vm.PendingLoans.Add(new Loan
                    {
                        LoanId = r.GetInt32(0),
                        CustomerId = r.GetInt32(1),
                        CustomerName = r.GetString(2),
                        CustomerEmail = r.GetString(3),
                        LoanType = r.GetString(4),
                        LoanAmount = r.GetDecimal(5),
                        TenureMonths = r.GetInt32(6),
                        InterestRate = r.GetDecimal(7),
                        MonthlyEMI = r.GetDecimal(8),
                        TotalRepayment = r.GetDecimal(9),
                        MonthlyIncome = r.GetDecimal(10),
                        EmploymentType = r.GetString(11),
                        Purpose = r.GetString(12),
                        CreditScore = r.GetInt32(13),
                        Status = r.GetString(14),
                        CreatedAt = r.GetDateTime(21)
                    });
                }
            }
            vm.PendingLoanApprovals = vm.PendingLoans.Count;

            // Inactive Accounts (No transaction > 6 months rule)
            string inactQuery = @"
                SELECT a.AccountId, a.CustomerId, a.AccountNumber, a.AccountTypeId, t.TypeName, a.Balance, a.Currency, a.Status, a.IFSCCode, a.BranchName, a.OpenedDate, a.LastActivityDate, u.FullName
                FROM Accounts a
                JOIN AccountTypes t ON a.AccountTypeId = t.AccountTypeId
                JOIN Customers c ON a.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE a.LastActivityDate < (CURRENT_TIMESTAMP - INTERVAL '6 months') OR a.Status IN ('Inactive', 'Frozen')";
            using (var cmd = new NpgsqlCommand(inactQuery, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    vm.InactiveAccounts.Add(new Account
                    {
                        AccountId = r.GetInt32(0),
                        CustomerId = r.GetInt32(1),
                        AccountNumber = r.GetString(2),
                        AccountTypeId = r.GetInt32(3),
                        AccountTypeName = r.GetString(4),
                        Balance = r.GetDecimal(5),
                        Currency = r.GetString(6),
                        Status = r.GetString(7),
                        OpenedDate = r.GetDateTime(10),
                        LastActivityDate = r.GetDateTime(11),
                        CustomerName = r.GetString(12)
                    });
                }
            }
            vm.InactiveAccountsCount = vm.InactiveAccounts.Count;

            // Fraud Alerts
            string fraudQuery = @"
                SELECT f.AlertId, f.AccountId, a.AccountNumber, u.FullName, f.TransactionId, f.TriggerRule, f.RiskLevel, f.Description, f.Status, f.ReviewedBy, f.ReviewRemarks, f.CreatedAt, f.ReviewedAt
                FROM FraudAlerts f
                JOIN Accounts a ON f.AccountId = a.AccountId
                JOIN Customers c ON a.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE f.Status IN ('New', 'Under Review') ORDER BY f.CreatedAt DESC";
            using (var cmd = new NpgsqlCommand(fraudQuery, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    vm.BranchFraudAlerts.Add(new FraudAlert
                    {
                        AlertId = r.GetInt32(0),
                        AccountId = r.GetInt32(1),
                        AccountNumber = r.GetString(2),
                        CustomerName = r.GetString(3),
                        TriggerRule = r.GetString(5),
                        RiskLevel = r.GetString(6),
                        Description = r.GetString(7),
                        Status = r.GetString(8),
                        CreatedAt = r.GetDateTime(11)
                    });
                }
            }
            vm.OpenFraudAlerts = vm.BranchFraudAlerts.Count;

            return vm;
        }

        public AdminDashboardViewModel GetAdminDashboard()
        {
            var vm = new AdminDashboardViewModel();
            using var conn = GetConnection();
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM Customers", conn))
                vm.TotalCustomers = Convert.ToInt32(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM Employees", conn))
                vm.TotalEmployees = Convert.ToInt32(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM Accounts", conn))
                vm.TotalAccounts = Convert.ToInt32(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM Transactions", conn))
                vm.TotalTransactions = Convert.ToInt32(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(Balance), 0) FROM Accounts", conn))
                vm.TotalDepositsVolume = Convert.ToDecimal(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(LoanAmount), 0) FROM Loans WHERE Status = 'Active'", conn))
                vm.TotalLoansVolume = Convert.ToDecimal(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM FraudAlerts WHERE Status IN ('New', 'Under Review')", conn))
                vm.ActiveFraudAlerts = Convert.ToInt32(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM Customers WHERE KYCStatus = 'Pending'", conn))
                vm.PendingKYCs = Convert.ToInt32(cmd.ExecuteScalar());

            using (var cmd = new NpgsqlCommand("SELECT COUNT(1) FROM ServiceRequests WHERE Status = 'Pending'", conn))
                vm.PendingServiceRequests = Convert.ToInt32(cmd.ExecuteScalar());

            // Recent Audit Logs
            string auditQuery = "SELECT  LogId, UserId, Username, Role, Action, Description, IPAddress, Timestamp FROM AuditLogs  ORDER BY Timestamp DESC LIMIT 10";
            using (var cmd = new NpgsqlCommand(auditQuery, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    vm.RecentAuditLogs.Add(new AuditLog
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
            }

            // Recent Transactions
            string txnQuery = @"
                SELECT  t.TransactionId, t.TransactionReference, t.AccountId, a.AccountNumber, t.TransactionType,
                                   t.Amount, t.BalanceAfter, t.DebitOrCredit, t.Category, t.Description,
                                   t.DestinationAccount, t.BeneficiaryName, t.PaymentMethod, t.Status, t.TransactionDate
                FROM Transactions t
                JOIN Accounts a ON t.AccountId = a.AccountId
                 ORDER BY t.TransactionDate DESC LIMIT 10";
            using (var cmd = new NpgsqlCommand(txnQuery, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    vm.RecentTransactions.Add(new Transaction
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
                        PaymentMethod = r.GetString(12),
                        Status = r.GetString(13),
                        TransactionDate = r.GetDateTime(14)
                    });
                }
            }

            return vm;
        }

        #endregion

        #region System Operation & Lab Helpers (CRUD / Datatable / Reports)

        public DataTable GetCustomerReportDataTable(DateTime? fromDate = null, DateTime? toDate = null)
        {
            string query = @"
                SELECT c.CustomerId, c.CustomerCode, u.FullName, u.Email, u.MobileNumber, c.PAN, c.KYCStatus, c.Segment, c.CreatedAt,
                       COALESCE((SELECT SUM(Balance) FROM Accounts a WHERE a.CustomerId = c.CustomerId), 0) AS TotalBalance
                FROM Customers c
                JOIN Users u ON c.UserId = u.UserId
                WHERE (@FromDate IS NULL OR c.CreatedAt >= @FromDate)
                  AND (@ToDate IS NULL OR c.CreatedAt <= @ToDate)
                ORDER BY c.CreatedAt DESC";

            var pFrom = new NpgsqlParameter("@FromDate", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = (object?)fromDate ?? DBNull.Value };
            var pTo = new NpgsqlParameter("@ToDate", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = (object?)toDate ?? DBNull.Value };
            var parameters = new[] { pFrom, pTo };

            return ExecuteDataTable(query, parameters);
        }

        public DataTable GetTransactionReportDataTable(DateTime? fromDate = null, DateTime? toDate = null, string? category = null)
        {
            string query = @"
                SELECT t.TransactionReference, a.AccountNumber, u.FullName AS CustomerName, t.TransactionType,
                       t.Amount, t.DebitOrCredit, t.Category, t.Description, t.PaymentMethod, t.Status, t.TransactionDate
                FROM Transactions t
                JOIN Accounts a ON t.AccountId = a.AccountId
                JOIN Customers c ON a.CustomerId = c.CustomerId
                JOIN Users u ON c.UserId = u.UserId
                WHERE (@FromDate IS NULL OR t.TransactionDate >= @FromDate)
                  AND (@ToDate IS NULL OR t.TransactionDate <= @ToDate)
                  AND (@Category IS NULL OR t.Category = @Category)
                ORDER BY t.TransactionDate DESC";

            var pFrom2 = new NpgsqlParameter("@FromDate", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = (object?)fromDate ?? DBNull.Value };
            var pTo2 = new NpgsqlParameter("@ToDate", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = (object?)toDate ?? DBNull.Value };
            var pCat = new NpgsqlParameter("@Category", NpgsqlTypes.NpgsqlDbType.Text) { Value = (object?)category ?? DBNull.Value };
            var parameters = new[] { pFrom2, pTo2, pCat };

            return ExecuteDataTable(query, parameters);
        }

        #endregion
    }
}







