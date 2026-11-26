**# BharatTrust Bank (BharatBank)

A role-based online banking simulation web application developed for the Semester 5 .NET Technology curriculum (Mumbai University). The project demonstrates core ASP.NET Core MVC, pure ADO.NET database operations, layered role-based access control (RBAC), and RESTful API development using .NET 8 and PostgreSQL.**
=======
A role-based online banking simulation web application. .NET Technology curriculum (Marwadi University). The project demonstrates core ASP.NET Core MVC, pure ADO.NET database operations, layered role-based access control (RBAC), and RESTful API development using .NET 8 and PostgreSQL.

---

## Table of Contents

- [Overview](#overview)
- [System Architecture](#system-architecture)
- [Project Structure](#project-structure)
- [Key Features](#key-features)
  - [Customer Portal](#customer-portal)
  - [Employee Portal](#employee-portal)
  - [Manager Portal](#manager-portal)
  - [Administrator Portal](#administrator-portal)
  - [REST API Services](#rest-api-services)
- [Technology Stack](#technology-stack)
- [Security & Authentication](#security--authentication)
- [Database Architecture](#database-architecture)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Database Setup](#database-setup)
  - [Configuration](#configuration)
  - [Running the Web Application](#running-the-web-application)
  - [Running the REST API](#running-the-rest-api)
- [Demo Credentials](#demo-credentials)
- [API Reference](#api-reference)
- [Academic Disclaimer](#academic-disclaimer)

---

## Overview

BharatTrust Bank is a full-stack banking simulation designed to model retail and branch banking operations in an Indian commercial banking environment. Rather than relying on an Object-Relational Mapper (ORM), the data access layer is intentionally built with direct **ADO.NET (`Npgsql`)** to demonstrate raw relational data access, parameterized command handling, transaction isolation, and tabular mapping as required by the university syllabus.

The system is split into two primary runnable applications:
1. **BharatTrustBank.Web**: An ASP.NET Core MVC portal with dedicated Areas for Customers, Employees, Managers, and Admins.
2. **BharatTrustBank.API**: A companion ASP.NET Core Web API providing authenticated endpoints with Swagger documentation.

---

## System Architecture

```
+-----------------------------------------------------------------------+
|                             Client Tier                               |
|        Browser (MVC Views, Bootstrap 5, Chart.js, Three.js)           |
+-----------------------------------------------------------------------+
                                   |
                                   v
+-----------------------------------------------------------------------+
|                    BharatTrustBank.Web (ASP.NET Core 8)               |
|                                                                       |
|   +-------------------+  +-------------------+  +------------------+  |
|   |   Public Pages    |  |  Session & RBAC   |  |  DatabaseHelper  |  |
|   |  Home, Rates,     |  |  Filters & Auth   |  |   (Pure ADO.NET) |  |
|   |  Account Auth     |  |  Handlers         |  |                  |  |
|   +-------------------+  +-------------------+  +------------------+  |
|                                                                       |
|   +---------------------------------------------------------------+   |
|   |                        MVC Areas                              |   |
|   |  /Customer  |  /Employee  |  /Manager  |  /Admin              |   |
|   +---------------------------------------------------------------+   |
+-----------------------------------------------------------------------+
                                   |
                                   v
+-----------------------------------------------------------------------+
|                Database Tier (PostgreSQL / Supabase)                  |
|          Tables, Constraints, Foreign Keys, Audit Tracing             |
+-----------------------------------------------------------------------+
                                   ^
                                   |
+-----------------------------------------------------------------------+
|                    BharatTrustBank.API (REST Endpoints)               |
|          Swagger UI | X-Api-Key Protection | JSON Payloads            |
+-----------------------------------------------------------------------+
```

---

## Project Structure

```
Bharat_Bank_.Net_Mini_Project/
|
|-- BharatTrustBank.Web/              # ASP.NET Core MVC web application
|   |-- Areas/
|   |   |-- Customer/                 # Accounts, Transfers, Deposits, Cards, Support
|   |   |-- Employee/                 # KYC verification, customer requests, complaints
|   |   |-- Manager/                  # Loan approvals, fraud alerts, rate settings
|   |   |-- Admin/                    # User management, audit logs, system configs
|   |-- Controllers/                  # Public controllers (Home, Account authentication)
|   |-- Filters/                      # RoleAuthorizeAttribute for RBAC enforcement
|   |-- Helpers/                      # Password hashing (PBKDF2/SHA-256) and session utilities
|   |-- Models/                       # View models and data transfer objects
|   |-- Services/                     # DatabaseHelper.cs (ADO.NET implementation)
|   |-- Views/                        # Razor views grouped by Area and Controller
|   |-- wwwroot/                      # Static assets: custom CSS, JS, Bootstrap, libraries
|   |-- appsettings.json              # Connection strings and bank settings
|   `-- Program.cs                    # Application pipeline, session, and route configuration
|
|-- BharatTrustBank.API/              # ASP.NET Core REST API
|   |-- Controllers/                  # Accounts, Customers, Loans, Transactions, etc.
|   |-- Security/                     # ApiKeyAuthorizationFilter (X-Api-Key validation)
|   |-- appsettings.json              # API settings and database connection
|   `-- Program.cs                    # Swagger configuration and middleware pipeline
|
|-- DbSetup/                          # Standalone console utility for database provisioning
|   |-- Program.cs                    # Executes schema and seed scripts against target PostgreSQL
|   |-- Database_Postgres.sql         # Table creation DDL script
|   `-- SeedData_Postgres.sql         # Demo records, system settings, sample users
|
`-- BharatTrustBank.Database/         # Database scripts archive
    |-- Database.sql                  # T-SQL schema reference
    |-- SeedData.sql                  # T-SQL seed reference
    `-- StoredProcedures.sql          # Stored procedure definitions
```

---

## Key Features

### Customer Portal
- **Account Summary & Statements**: View real-time balances for Savings, Current, and Salary accounts, with mini-statements and date-filtered transaction histories.
- **Fund Transfers**: Simulated transactions via IMPS, NEFT, RTGS, and UPI with beneficiary management and OTP verification workflows.
- **Fixed & Recurring Deposits**: FD and RD calculator with interest computation, tenure selection, and booking management.
- **Loan Applications**: Apply for Personal, Home, Car, and Education loans; calculate EMIs; track approval statuses and payment schedules.
- **Card Controls**: Manage Debit and Credit cards, set transaction limits, change PINs, and toggle card status (Active/Blocked).
- **Utility Bill Payments**: Simulated biller integration for Electricity, Mobile, DTH, Water, and Broadband.
- **Visual Analytics**: Interactive expense distribution and monthly spending trends rendered using Chart.js.
- **Support & AI Assistant**: Submit and track customer support tickets, browse FAQs, and interact with a simulated banking query assistant.

### Employee Portal
- **Branch Queue Dashboard**: Real-time snapshot of pending customer requests and verification queues.
- **KYC & Customer Verification**: Review customer documents, verify user details, and update verification flags.
- **Service Request Processing**: Handle cheque book requests, debit card replacements, and profile updates.
- **Grievance Handling**: Track, assign, and resolve customer complaints with internal status logs.

### Manager Portal
- **Branch KPI Dashboard**: Track branch deposit-to-loan ratios, liquidity indicators, and overall branch volume.
- **Loan Underwriting**: Review submitted loan applications, evaluate debt-to-income and credit parameters, and approve or reject applications.
- **Interest Rate Management**: Configure base interest rates for deposit products and lending categories.
- **Fraud & Anomaly Monitoring**: Review high-value transfers, abnormal transaction patterns, and system-flagged alerts.
- **Account Actions**: Restrict, freeze, or unfreeze branch accounts based on compliance requirements.

### Administrator Portal
- **User Management**: Create, edit, activate, deactivate, or reset accounts across all roles (Customer, Employee, Manager, Admin).
- **Audit Logging**: Review system activity logs, authentication timestamps, client IP addresses, and failed login attempts.
- **System Settings**: Global configuration for bank IFSC codes, branch metadata, minimum balance thresholds, and simulation modes.
- **CMS Management**: Update promotional banners, public rate sheets, system FAQs, and banking offers shown on public pages.

### REST API Services
- Dedicated endpoints for external or mobile consumption covering accounts, transactions, customer profiles, loans, notifications, and support tickets.
- Secured via constant-time header comparison using an API key (`X-Api-Key`).
- Interactive OpenAPI documentation rendered via Swagger UI at `/` (root route).

---

## Technology Stack

| Layer | Technologies |
|---|---|
| **Framework** | .NET 8.0 (C# 12) |
| **Web Architecture** | ASP.NET Core MVC (with Areas) |
| **API Architecture** | ASP.NET Core Web API with Swagger/OpenAPI |
| **Data Access** | Pure ADO.NET (`Npgsql` 8.0.3), Parameterized SQL Queries |
| **Database** | PostgreSQL 15+ (Supports local instances and Supabase) |
| **Authentication** | ASP.NET Core Session State, Salted Hash Verification, RBAC Action Filters |
| **Frontend** | HTML5, Razor Views, CSS3, Bootstrap 5.3, Bootstrap Icons |
| **Client-Side Scripts**| JavaScript (ES6+), jQuery, Chart.js, Three.js (Background Visuals) |

---

## Security & Authentication

- **Password Hashing**: Passwords are saved as cryptographic hashes combined with an application-level salt (`BTB_SALT_2026`). Plaintext credentials are never stored.
- **Role-Based Authorization**: MVC actions are protected with a custom filter attribute (`[RoleAuthorize("Admin", "Manager")]`) that validates session tokens against the current user role before execution, preventing direct URL traversal attacks.
- **Account Lockout Policy**: Tracks failed authentication attempts and locks accounts once the configurable threshold (default: 5 attempts) is exceeded.
- **API Security**: The REST API enforces header-based authentication via `X-Api-Key` validated using `CryptographicOperations.FixedTimeEquals` to prevent timing attacks.
- **Session Protection**: Utilizes HTTP-only essential cookies (`.BharatTrustBank.Session`) with a 30-minute idle timeout.

---

## Database Architecture

The schema comprises over 30 normalized tables modeling core banking domains:

- **Identity & Access**: `Users`, `Employees`, `Customers`, `LoginHistory`, `Devices`, `SecurityQuestions`
- **Core Banking**: `AccountTypes`, `Accounts`, `Beneficiaries`, `Transactions`, `ScheduledTransfers`
- **Deposits & Credit**: `FixedDeposits`, `RecurringDeposits`, `Loans`, `LoanPayments`
- **Cards & Instruments**: `DebitCards`, `CreditCards`, `ChequeRequests`
- **Customer Services**: `ServiceRequests`, `BillPayments`, `Investments`, `SupportTickets`, `ChatMessages`, `Notifications`
- **Operations & Security**: `FraudAlerts`, `AuditLogs`, `SystemSettings`, `InterestRates`, `CurrencyRates`, `Offers`, `FAQs`

---

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
- [PostgreSQL 15+](https://www.postgresql.org/download/) running locally, or a managed cloud PostgreSQL instance (e.g., Supabase, Neon)

### Database Setup

#### Option A: Using the `DbSetup` Console Application (Recommended)

1. Open a terminal and navigate to the `DbSetup` directory:
   ```bash
   cd Bharat_Bank_.Net_Mini_Project/DbSetup
   ```
2. Set the connection string environment variable (or let it run with default parameters against local PostgreSQL):
   ```bash
   # Windows PowerShell
   $env:DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=bharattrustbank;Username=postgres;Password=your_password;"

   # Linux / macOS
   export DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=bharattrustbank;Username=postgres;Password=your_password;"
   ```
3. Run the setup utility to execute both the DDL schema and seed data scripts:
   ```bash
   dotnet run
   ```

#### Option B: Manual Execution via psql / pgAdmin

1. Create a database named `bharattrustbank`.
2. Execute the scripts in the following order:
   ```bash
   psql -U postgres -d bharattrustbank -f Bharat_Bank_.Net_Mini_Project/DbSetup/Database_Postgres.sql
   psql -U postgres -d bharattrustbank -f Bharat_Bank_.Net_Mini_Project/DbSetup/SeedData_Postgres.sql
   ```

### Configuration

Update your database connection string in:
- `BharatTrustBank.Web/appsettings.json`
- `BharatTrustBank.API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bharattrustbank;Username=postgres;Password=your_password;Include Error Detail=true;"
  }
}
```

### Running the Web Application

From the root project folder:

```bash
cd Bharat_Bank_.Net_Mini_Project/BharatTrustBank.Web
dotnet restore
dotnet run
```

Open your browser and navigate to:
```
https://localhost:5001
```
*(Or the HTTP port assigned in `Properties/launchSettings.json`)*

### Running the REST API

Open a separate terminal window:

```bash
cd Bharat_Bank_.Net_Mini_Project/BharatTrustBank.API
dotnet restore
dotnet run
```

Navigate to:
```
https://localhost:5002/
```
The Swagger UI will display the list of available endpoints.

---

## Demo Credentials

The database seed script includes pre-configured accounts for testing each portal:

| Role | Username | Password | Notes |
|---|---|---|---|
| **Administrator** | `admin` | `Admin@123` | Full system access, configuration, and audit logs |
| **Manager** | `manager.sharma` | `Password@123` | Branch underwriting, loan approvals, fraud logs |
| **Manager** | `manager.patel` | `Password@123` | Secondary manager account |
| **Employee** | `emp.vikram` | `Password@123` | KYC verification and customer ticket queue |
| **Employee** | `emp.priya` | `Password@123` | Customer support and request handling |
| **Customer** | `rahul.sharma` | `Password@123` | Primary test customer (Savings & Current accounts) |
| **Customer** | `sneha.patel` | `Password@123` | Secondary customer with active deposits & loans |
| **Customer** | `ananya.deshmukh` | `Password@123` | Customer with card and transfer history |

---

## API Reference

All requests to `BharatTrustBank.API` require the `X-Api-Key` header configured in `appsettings.json`.

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/accounts` | Retrieve active bank accounts |
| `GET` | `/api/accounts/{id}` | Retrieve specific account details and balance |
| `GET` | `/api/customers` | Fetch list of registered customers |
| `GET` | `/api/customers/{id}` | Fetch customer demographic and contact data |
| `GET` | `/api/transactions` | Query recent transactions across accounts |
| `POST` | `/api/transactions/transfer` | Submit an internal fund transfer |
| `GET` | `/api/loans` | View active loan facilities |
| `GET` | `/api/notifications` | Retrieve broadcast and user alerts |
| `GET` | `/api/support/faqs` | Retrieve public banking FAQs |
| `POST` | `/api/support/ticket` | Create a new customer support ticket |

---

**## Academic Disclaimer

This application is an educational banking simulation developed for the Mumbai University (Sem 5) .NET Technology course. It is not connected to real banking networks or payment processors. All financial transactions, OTP verifications, payment confirmations, and account balances are simulated for practical learning and project demonstration purposes.**
=======
This application is an educational banking simulation developed for the Marwadi University .NET Technology course. It is not connected to real banking networks or payment processors. All financial transactions, OTP verifications, payment confirmations, and account balances are simulated for practical learning and project demonstration purposes.
