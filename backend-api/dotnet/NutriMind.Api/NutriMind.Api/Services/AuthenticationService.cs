using Microsoft.Extensions.Configuration;
using NutriMind.Api.Helpers;
using System.Security.Claims;

namespace NutriMind.Api.Services
{
    public interface IAuthService
    {
        string GenerateToken(string userId, string email);
        ClaimsPrincipal? ValidateToken(string token);
        string? GetUserIdFromRequest(Microsoft.Azure.Functions.Worker.Http.HttpRequestData request);
        string? GetUserIdFromClaims(ClaimsPrincipal principal);
        bool IsAuthenticated(Microsoft.Azure.Functions.Worker.Http.HttpRequestData request);
    }

    public class AuthService : IAuthService
    {
        private readonly JWTHelper _jwtHelper;

        public AuthService(IConfiguration configuration)
        {
            var secretKey = configuration["JWT_SECRET_KEY"] ?? "your-super-secret-key-change-in-production";
            var issuer = configuration["JWT_ISSUER"] ?? "nutrimind-api";
            var audience = configuration["JWT_AUDIENCE"] ?? "nutrimind-app";

            _jwtHelper = new JWTHelper(secretKey, issuer, audience);
        }

        public string GenerateToken(string userId, string email)
        {
            return _jwtHelper.GenerateToken(userId, email);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            return _jwtHelper.ValidateToken(token);
        }

        public string? GetUserIdFromRequest(Microsoft.Azure.Functions.Worker.Http.HttpRequestData request)
        {
            var authHeader = request.Headers.FirstOrDefault(h => h.Key.ToLower() == "authorization").Value?.FirstOrDefault();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            var token = authHeader["Bearer ".Length..].Trim();
            return _jwtHelper.GetUserIdFromToken(token);
        }

        public string? GetUserIdFromClaims(ClaimsPrincipal principal)
        {
            return principal?.FindFirst("user_id")?.Value;
        }

        public bool IsAuthenticated(Microsoft.Azure.Functions.Worker.Http.HttpRequestData request)
        {
            var userId = GetUserIdFromRequest(request);
            return !string.IsNullOrEmpty(userId);
        }
    }
}
