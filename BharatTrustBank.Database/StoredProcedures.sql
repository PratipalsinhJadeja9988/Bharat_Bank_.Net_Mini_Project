-- =============================================
-- BharatTrust Bank - Stored Procedures
-- ADO.NET Banking Transactions & Procedures
-- =============================================

USE BharatTrustBankDB;
GO

-- 1. Money Transfer Stored Procedure with ACID Transaction
IF OBJECT_ID('sp_TransferMoney', 'P') IS NOT NULL DROP PROCEDURE sp_TransferMoney;
GO
CREATE PROCEDURE sp_TransferMoney
    @FromAccountId INT,
    @ToAccountNumber NVARCHAR(20),
    @BeneficiaryName NVARCHAR(100),
    @Amount DECIMAL(18,2),
    @TransferType NVARCHAR(10), -- 'IMPS', 'NEFT', 'RTGS', 'UPI'
    @Description NVARCHAR(255),
    @TransactionRef NVARCHAR(30) OUTPUT,
    @StatusCode INT OUTPUT, -- 0 = Success, 1 = Account Not Active, 2 = Insufficient Balance, 3 = Invalid Destination, 4 = Error
    @StatusMessage NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Validate Source Account
        DECLARE @FromBalance DECIMAL(18,2), @FromStatus NVARCHAR(20), @FromCustId INT;
        SELECT @FromBalance = Balance, @FromStatus = Status, @FromCustId = CustomerId
        FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountId = @FromAccountId;

        IF @FromBalance IS NULL OR @FromStatus <> 'Active'
        BEGIN
            SET @StatusCode = 1;
            SET @StatusMessage = 'Source account is not active or does not exist.';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Amount <= 0
        BEGIN
            SET @StatusCode = 2;
            SET @StatusMessage = 'Transfer amount must be greater than zero.';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @FromBalance < @Amount
        BEGIN
            SET @StatusCode = 2;
            SET @StatusMessage = 'Insufficient balance in source account.';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 2. Deduct from Source Account
        UPDATE Accounts 
        SET Balance = Balance - @Amount, LastActivityDate = GETDATE()
        WHERE AccountId = @FromAccountId;

        DECLARE @NewFromBalance DECIMAL(18,2) = @FromBalance - @Amount;
        
        -- Generate Transaction Reference (e.g. TXN20260820xxxx)
        SET @TransactionRef = 'TXN' + CONVERT(NVARCHAR(8), GETDATE(), 112) + RIGHT(CAST(ABS(CHECKSUM(NEWID())) AS NVARCHAR(10)), 6);

        -- Record Debit Transaction for Sender
        INSERT INTO Transactions (
            TransactionReference, AccountId, TransactionType, Amount, BalanceAfter,
            DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status, TransactionDate
        ) VALUES (
            @TransactionRef, @FromAccountId, 'Transfer', @Amount, @NewFromBalance,
            'Debit', 'Transfer', @Description, @ToAccountNumber, @BeneficiaryName, @TransferType, 'Completed', GETDATE()
        );

        -- 3. If Destination is an internal BharatTrust Bank account, Credit it
        DECLARE @ToAccountId INT, @ToBalance DECIMAL(18,2), @ToCustId INT;
        SELECT @ToAccountId = AccountId, @ToBalance = Balance, @ToCustId = CustomerId
        FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountNumber = @ToAccountNumber AND Status = 'Active';

        IF @ToAccountId IS NOT NULL
        BEGIN
            UPDATE Accounts
            SET Balance = Balance + @Amount, LastActivityDate = GETDATE()
            WHERE AccountId = @ToAccountId;

            DECLARE @NewToBalance DECIMAL(18,2) = @ToBalance + @Amount;
            DECLARE @CreditTxnRef NVARCHAR(30) = 'TXN' + CONVERT(NVARCHAR(8), GETDATE(), 112) + RIGHT(CAST(ABS(CHECKSUM(NEWID())) AS NVARCHAR(10)), 6);

            INSERT INTO Transactions (
                TransactionReference, AccountId, TransactionType, Amount, BalanceAfter,
                DebitOrCredit, Category, Description, DestinationAccount, BeneficiaryName, PaymentMethod, Status, TransactionDate
            ) VALUES (
                @CreditTxnRef, @ToAccountId, 'Transfer', @Amount, @NewToBalance,
                'Credit', 'Transfer', 'Received from A/C ' + (SELECT AccountNumber FROM Accounts WHERE AccountId = @FromAccountId), 
                (SELECT AccountNumber FROM Accounts WHERE AccountId = @FromAccountId), 'Self/Customer', @TransferType, 'Completed', GETDATE()
            );

            -- Notification for recipient
            INSERT INTO Notifications (UserId, Title, Message, NotificationType)
            SELECT UserId, 'Money Received', 'Account credited with INR ' + CAST(@Amount AS NVARCHAR(20)) + ' via ' + @TransferType, 'Transaction'
            FROM Customers WHERE CustomerId = @ToCustId;
        END

        -- Notification for sender
        INSERT INTO Notifications (UserId, Title, Message, NotificationType)
        SELECT UserId, 'Money Debited', 'Account debited with INR ' + CAST(@Amount AS NVARCHAR(20)) + ' for transfer to ' + @BeneficiaryName, 'Transaction'
        FROM Customers WHERE CustomerId = @FromCustId;

        -- Rule-based Fraud Check: If transfer amount > 200000, trigger fraud alert
        IF @Amount >= 200000
        BEGIN
            INSERT INTO FraudAlerts (AccountId, TriggerRule, RiskLevel, Description, Status)
            VALUES (@FromAccountId, 'High Value Transaction Rule', 'High', 'Transfer amount exceeds INR 2,00,000 threshold (' + CAST(@Amount AS NVARCHAR(20)) + ')', 'New');
        END

        -- Audit Log
        INSERT INTO AuditLogs (UserId, Username, Role, Action, Description)
        SELECT u.UserId, u.Username, u.Role, 'Fund Transfer', 'Transferred INR ' + CAST(@Amount AS NVARCHAR(20)) + ' via ' + @TransferType + ' to ' + @BeneficiaryName
        FROM Customers c JOIN Users u ON c.UserId = u.UserId WHERE c.CustomerId = @FromCustId;

        COMMIT TRANSACTION;
        SET @StatusCode = 0;
        SET @StatusMessage = 'Transfer completed successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @StatusCode = 4;
        SET @StatusMessage = ERROR_MESSAGE();
    END CATCH
END
GO

-- 2. Deposit Simulation Stored Procedure
IF OBJECT_ID('sp_DepositMoney', 'P') IS NOT NULL DROP PROCEDURE sp_DepositMoney;
GO
CREATE PROCEDURE sp_DepositMoney
    @AccountId INT,
    @Amount DECIMAL(18,2),
    @Description NVARCHAR(255),
    @TransactionRef NVARCHAR(30) OUTPUT,
    @StatusCode INT OUTPUT,
    @StatusMessage NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Balance DECIMAL(18,2), @Status NVARCHAR(20), @CustId INT;
        SELECT @Balance = Balance, @Status = Status, @CustId = CustomerId
        FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountId = @AccountId;

        IF @Balance IS NULL OR @Status <> 'Active'
        BEGIN
            SET @StatusCode = 1;
            SET @StatusMessage = 'Account is not active or does not exist.';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Amount <= 0
        BEGIN
            SET @StatusCode = 2;
            SET @StatusMessage = 'Deposit amount must be greater than zero.';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE Accounts 
        SET Balance = Balance + @Amount, LastActivityDate = GETDATE()
        WHERE AccountId = @AccountId;

        DECLARE @NewBalance DECIMAL(18,2) = @Balance + @Amount;
        SET @TransactionRef = 'DEP' + CONVERT(NVARCHAR(8), GETDATE(), 112) + RIGHT(CAST(ABS(CHECKSUM(NEWID())) AS NVARCHAR(10)), 6);

        INSERT INTO Transactions (
            TransactionReference, AccountId, TransactionType, Amount, BalanceAfter,
            DebitOrCredit, Category, Description, PaymentMethod, Status, TransactionDate
        ) VALUES (
            @TransactionRef, @AccountId, 'Deposit', @Amount, @NewBalance,
            'Credit', 'Cash', @Description, 'Online Deposit', 'Completed', GETDATE()
        );

        INSERT INTO Notifications (UserId, Title, Message, NotificationType)
        SELECT UserId, 'Deposit Successful', 'Amount of INR ' + CAST(@Amount AS NVARCHAR(20)) + ' deposited to your account.', 'Transaction'
        FROM Customers WHERE CustomerId = @CustId;

        COMMIT TRANSACTION;
        SET @StatusCode = 0;
        SET @StatusMessage = 'Deposit successful.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @StatusCode = 4;
        SET @StatusMessage = ERROR_MESSAGE();
    END CATCH
END
GO

-- 3. Withdrawal Simulation Stored Procedure
IF OBJECT_ID('sp_WithdrawMoney', 'P') IS NOT NULL DROP PROCEDURE sp_WithdrawMoney;
GO
CREATE PROCEDURE sp_WithdrawMoney
    @AccountId INT,
    @Amount DECIMAL(18,2),
    @Description NVARCHAR(255),
    @TransactionRef NVARCHAR(30) OUTPUT,
    @StatusCode INT OUTPUT,
    @StatusMessage NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Balance DECIMAL(18,2), @Status NVARCHAR(20), @CustId INT;
        SELECT @Balance = Balance, @Status = Status, @CustId = CustomerId
        FROM Accounts WITH (UPDLOCK, HOLDLOCK) WHERE AccountId = @AccountId;

        IF @Balance IS NULL OR @Status <> 'Active'
        BEGIN
            SET @StatusCode = 1;
            SET @StatusMessage = 'Account is not active or does not exist.';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Amount <= 0
        BEGIN
            SET @StatusCode = 2;
            SET @StatusMessage = 'Withdrawal amount must be greater than zero.';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Balance < @Amount
        BEGIN
            SET @StatusCode = 2;
            SET @StatusMessage = 'Insufficient account balance.';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        UPDATE Accounts 
        SET Balance = Balance - @Amount, LastActivityDate = GETDATE()
        WHERE AccountId = @AccountId;

        DECLARE @NewBalance DECIMAL(18,2) = @Balance - @Amount;
        SET @TransactionRef = 'WTH' + CONVERT(NVARCHAR(8), GETDATE(), 112) + RIGHT(CAST(ABS(CHECKSUM(NEWID())) AS NVARCHAR(10)), 6);

        INSERT INTO Transactions (
            TransactionReference, AccountId, TransactionType, Amount, BalanceAfter,
            DebitOrCredit, Category, Description, PaymentMethod, Status, TransactionDate
        ) VALUES (
            @TransactionRef, @AccountId, 'Withdrawal', @Amount, @NewBalance,
            'Debit', 'Cash', @Description, 'ATM Withdrawal', 'Completed', GETDATE()
        );

        INSERT INTO Notifications (UserId, Title, Message, NotificationType)
        SELECT UserId, 'Withdrawal Alert', 'Amount of INR ' + CAST(@Amount AS NVARCHAR(20)) + ' withdrawn from your account.', 'Transaction'
        FROM Customers WHERE CustomerId = @CustId;

        COMMIT TRANSACTION;
        SET @StatusCode = 0;
        SET @StatusMessage = 'Withdrawal successful.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @StatusCode = 4;
        SET @StatusMessage = ERROR_MESSAGE();
    END CATCH
END
GO
