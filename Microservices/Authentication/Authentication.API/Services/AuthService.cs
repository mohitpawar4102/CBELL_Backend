using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using YourNamespace.Models;

namespace YourNamespace.Services
{
    public class AuthService
    {
        private readonly TokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Example users stored in-memory (Replace with database lookup)
        private static readonly Dictionary<string, string> users = new()
        {
            { "admin", "password123" },
            { "user", "mypassword" }
        };

        public AuthService(TokenService tokenService, IHttpContextAccessor httpContextAccessor)
        {
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        // Handle user login
        public async Task<(bool success, string message, string token)> LoginAsync(LoginModel login)
        {
            if (login == null || string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
            {
                return (false, "Invalid request data", null);
            }

            if (users.TryGetValue(login.Username, out var storedPassword) && storedPassword == login.Password)
            {
                var token = _tokenService.GenerateToken(login.Username);
                SetAuthTokenCookie(token);
                return (true, "Login successful", token);
            }

            return (false, "Invalid username or password", null);
        }

        // Handle user logout
        public (bool success, string message) Logout()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                httpContext.Response.Cookies.Delete("AuthToken");
            }
            return (true, "Logout successful");
        }

        // Handle Google Login (OAuth)
        public async Task<(bool success, string message, string token, string email, string name)> GoogleLoginAsync()
        {
            var authenticateResult = await _httpContextAccessor.HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
                return (false, "Google authentication failed", null, null, null);

            var claims = authenticateResult.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return (false, "Google authentication failed", null, null, null);

            var token = _tokenService.GenerateToken(email);
            SetAuthTokenCookie(token);

            return (true, "Google login successful", token, email, name);
        }

        // Set auth token as an HTTP-only cookie
        private void SetAuthTokenCookie(string token)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(60)
                };
                httpContext.Response.Cookies.Append("AuthToken", token, cookieOptions);
            }
        }
    }
}
