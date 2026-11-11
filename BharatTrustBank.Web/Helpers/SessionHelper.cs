using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BharatTrustBank.Web.Helpers
{
    public static class SessionHelper
    {
        public const string KeyUserId = "BTB_UserId";
        public const string KeyUsername = "BTB_Username";
        public const string KeyFullName = "BTB_FullName";
        public const string KeyUserRole = "BTB_UserRole";
        public const string KeyCustomerId = "BTB_CustomerId";
        public const string KeyEmployeeId = "BTB_EmployeeId";
        public const string KeyTermsAccepted = "BTB_TermsAccepted";

        public static void SetUserSession(ISession session, int userId, string username, string fullName, string role, int? customerId = null, int? employeeId = null)
        {
            session.SetInt32(KeyUserId, userId);
            session.SetString(KeyUsername, username);
            session.SetString(KeyFullName, fullName);
            session.SetString(KeyUserRole, role);
            if (customerId.HasValue) session.SetInt32(KeyCustomerId, customerId.Value);
            if (employeeId.HasValue) session.SetInt32(KeyEmployeeId, employeeId.Value);
        }

        public static void ClearUserSession(ISession session)
        {
            session.Clear();
        }

        public static bool IsLoggedIn(ISession session)
        {
            return session.GetInt32(KeyUserId).HasValue;
        }

        public static int? GetUserId(ISession session) => session.GetInt32(KeyUserId);
        public static string? GetUsername(ISession session) => session.GetString(KeyUsername);
        public static string? GetFullName(ISession session) => session.GetString(KeyFullName);
        public static string? GetUserRole(ISession session) => session.GetString(KeyUserRole);
        public static int? GetCustomerId(ISession session) => session.GetInt32(KeyCustomerId);
        public static int? GetEmployeeId(ISession session) => session.GetInt32(KeyEmployeeId);
    }
}
