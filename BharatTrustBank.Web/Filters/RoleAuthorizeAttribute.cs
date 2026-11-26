using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BharatTrustBank.Web.Helpers;
using System;
using System.Linq;

namespace BharatTrustBank.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public RoleAuthorizeAttribute(params string[] roles)
        {
            _allowedRoles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var userId = SessionHelper.GetUserId(session);
            var userRole = SessionHelper.GetUserRole(session);

            if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            {
                // Not logged in -> redirect to login
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl = context.HttpContext.Request.Path });
                return;
            }

            if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
            {
                // Unauthorized role access -> redirect to 403 Forbidden page or Home
                context.Result = new RedirectToActionResult("AccessDenied", "Home", new { area = "" });
            }
        }
    }
}
