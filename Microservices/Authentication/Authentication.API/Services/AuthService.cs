using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using YourNamespace.Library.Database;
using YourNamespace.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace YourNamespace.Services
{
    public class AuthService
    {
        private readonly TokenService _tokenService;
        private readonly MongoDbService _mongoDbService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public AuthService(TokenService tokenService, MongoDbService mongoDbService, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _tokenService = tokenService;
            _mongoDbService = mongoDbService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var existingUser = await _mongoDbService.Users
                .Find(u => u.Username == request.Username)
                .FirstOrDefaultAsync();
            if (existingUser != null)
                return new BadRequestObjectResult(new { message = "User already exists" });

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password)
            };

            await _mongoDbService.Users.InsertOneAsync(user);
            return new OkObjectResult(new { message = "Registration successful" });
        }

        public async Task<IActionResult> Login(LoginModel login)
        {
            // Console.WriteLine($"Received login request: {JsonSerializer.Serialize(login)}");

            var user = await _mongoDbService.Users
                .Find(u => u.Username == login.Username)
                .FirstOrDefaultAsync();

            if (user == null || !VerifyPassword(login.Password, user.PasswordHash))
                return new UnauthorizedObjectResult(new { message = "Invalid username or password" });

            // Generate local access and refresh tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Username);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Store refresh token in the database
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _mongoDbService.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

            // Store tokens in cookies
            SetAuthTokenCookie("LocalAccessToken", accessToken);
            SetAuthTokenCookie("LocalRefreshToken", refreshToken);

            // Return tokens in response as well
            return new OkObjectResult(new
            {
                message = "Login successful",
                // accessToken,
                // refreshToken,
            });
        }
        public async Task<IActionResult> Logout()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return new BadRequestObjectResult(new { message = "Invalid request" });

            // Retrieve the user's refresh token from the cookie
            if (context.Request.Cookies.TryGetValue("LocalRefreshToken", out var refreshToken))
            {
                // Find the user in the database using the refresh token
                var user = await _mongoDbService.Users.Find(u => u.RefreshToken == refreshToken).FirstOrDefaultAsync();
                if (user != null)
                {
                    // Invalidate the refresh token
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow;
                    await _mongoDbService.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
                }
            }

            // Remove all authentication-related cookies
            RemoveAuthTokenCookie("LocalAccessToken");
            RemoveAuthTokenCookie("LocalRefreshToken");
            RemoveAuthTokenCookie("GoogleAccessToken");
            RemoveAuthTokenCookie("GoogleRefreshToken");

            return new OkObjectResult(new { message = "Logout successful" });
        }

        private void RemoveAuthTokenCookie(string key)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Response.Cookies.Delete(key);
            }
        }


        public IActionResult InitiateGoogleLogin()
        {
            var redirectUri = _configuration["Google:RedirectUri"];

            // redirectUri = redirectUri.Replace("http://", "https://");

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

            return new ChallengeResult(GoogleDefaults.AuthenticationScheme, properties);
        }

        public async Task<IActionResult> GoogleLoginAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            var authenticateResult = await context!.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
                return new BadRequestObjectResult(new { message = "Google authentication failed" });

            var claims = authenticateResult.Principal?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return new BadRequestObjectResult(new { message = "Google authentication failed" });

            // Extract Google access and refresh tokens
            var googleAccessToken = authenticateResult.Properties?.GetTokenValue("access_token");
            var googleRefreshToken = authenticateResult.Properties?.GetTokenValue("refresh_token");

            // Generate your local tokens
            var accessToken = _tokenService.GenerateAccessToken(email);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Store local refresh token in database (for token refresh mechanism)
            var user = await _mongoDbService.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _mongoDbService.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
            }

            // Store tokens in cookies
            SetAuthTokenCookie("LocalAccessToken", accessToken);
            SetAuthTokenCookie("LocalRefreshToken", refreshToken);
            SetAuthTokenCookie("GoogleAccessToken", googleAccessToken);

            if (!string.IsNullOrEmpty(googleRefreshToken))
            {
                SetAuthTokenCookie("GoogleRefreshToken", googleRefreshToken);
            }

            // Return all tokens in the response
            return new OkObjectResult(new
            {
                message = "Google login successful",
                // googleAccessToken,
                // googleRefreshToken,
                // accessToken,
                // refreshToken,
                email,
                name
            });
        }

        private void SetAuthTokenCookie(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var context = _httpContextAccessor.HttpContext;
                context?.Response.Cookies.Append(key, value, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            return HashPassword(enteredPassword) == storedHash;
        }
    }
}
