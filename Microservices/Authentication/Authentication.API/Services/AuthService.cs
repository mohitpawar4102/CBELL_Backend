using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using YourNamespace.Library.Database;
using YourNamespace.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

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

        private IMongoCollection<User> GetUsersCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<User>("Users");
        }

        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var usersCollection = GetUsersCollection();
            var organizationsCollection = _mongoDbService.GetDatabase().GetCollection<BsonDocument>("OrganizationMst");

            // Check if user already exists
            var existingUser = await usersCollection.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
            if (existingUser != null)
                return new BadRequestObjectResult(new { message = "User already exists" });

            // Lookup Organization by code
            var organization = await organizationsCollection.Find(new BsonDocument
    {
        { "OrganizationCode", request.OrganizationCode },
        { "IsDeleted", false }
    }).FirstOrDefaultAsync();

            if (organization == null)
                return new BadRequestObjectResult(new { message = "Invalid organization code." });

            var organizationId = organization["_id"].AsObjectId.ToString(); // get ObjectId as string

            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = HashPassword(request.Password),
                OrganizationCode = request.OrganizationCode,
                OrganizationId = organizationId, // set from organization _id
                MFA = 1,
                UserStatus = 1,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
            };

            await usersCollection.InsertOneAsync(user);

            return new OkObjectResult(new { message = "Registration successful" });
        }
        public async Task<IActionResult> Login(LoginModel login)
        {
            var usersCollection = GetUsersCollection();
            var user = await usersCollection.Find(u => u.Email == login.Email).FirstOrDefaultAsync();

            if (user == null || !VerifyPassword(login.Password, user.PasswordHash))
                return new UnauthorizedObjectResult(new { message = "Invalid email or password" });

            var accessToken = _tokenService.GenerateAccessToken(user.Email);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);

            SetAuthTokenCookie("LocalAccessToken", accessToken);
            SetAuthTokenCookie("LocalRefreshToken", refreshToken);

            return new OkObjectResult(new
            {
                message = "Login successful",
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                organizationId = user.OrganizationId,
            });
        }

        public async Task<IActionResult> GetAllUsers()
        {
            var usersCollection = GetUsersCollection();

            var users = await usersCollection.Find(_ => true).ToListAsync();

            var response = users.Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.OrganizationCode,
                u.MFA,
                u.UserStatus,
                u.CreatedOn,
                u.UpdatedOn,
                u.OrganizationId
            });

            return new OkObjectResult(response);
        }

        public async Task<IActionResult> Logout()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return new BadRequestObjectResult(new { message = "Invalid request" });

            var usersCollection = GetUsersCollection();

            if (context.Request.Cookies.TryGetValue("LocalRefreshToken", out var refreshToken))
            {
                var user = await usersCollection.Find(u => u.RefreshToken == refreshToken).FirstOrDefaultAsync();
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow;
                    await usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
                }
            }

            RemoveAuthTokenCookie("LocalAccessToken");
            RemoveAuthTokenCookie("LocalRefreshToken");
            RemoveAuthTokenCookie("GoogleAccessToken");
            RemoveAuthTokenCookie("GoogleRefreshToken");

            return new OkObjectResult(new { message = "Logout successful" });
        }

        private void RemoveAuthTokenCookie(string key)
        {
            var context = _httpContextAccessor.HttpContext;
            context?.Response.Cookies.Delete(key);
        }

        public IActionResult InitiateGoogleLogin()
        {
            var redirectUri = _configuration["Google:RedirectUri"];

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

            var googleAccessToken = authenticateResult.Properties?.GetTokenValue("access_token");
            var googleRefreshToken = authenticateResult.Properties?.GetTokenValue("refresh_token");

            var accessToken = _tokenService.GenerateAccessToken(email);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var usersCollection = GetUsersCollection();
            var user = await usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
            }

            SetAuthTokenCookie("LocalAccessToken", accessToken);
            SetAuthTokenCookie("LocalRefreshToken", refreshToken);
            SetAuthTokenCookie("GoogleAccessToken", googleAccessToken);

            if (!string.IsNullOrEmpty(googleRefreshToken))
            {
                SetAuthTokenCookie("GoogleRefreshToken", googleRefreshToken);
            }

            return new OkObjectResult(new
            {
                message = "Google login successful",
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