using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BharatTrustBank.API.Security;

/// <summary>Protects this internal API until a user-scoped authentication scheme is introduced.</summary>
public sealed class ApiKeyAuthorizationFilter(IConfiguration configuration) : IAuthorizationFilter
{
    private const string HeaderName = "X-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        string? expectedKey = configuration["ApiSettings:InternalApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            context.Result = new ObjectResult(new { Message = "API access is not configured." }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var suppliedKey) ||
            !FixedTimeEquals(expectedKey, suppliedKey.ToString()))
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "A valid X-Api-Key header is required." });
        }
    }

    private static bool FixedTimeEquals(string expected, string supplied) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied));
}
