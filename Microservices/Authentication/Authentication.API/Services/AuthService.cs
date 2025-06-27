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
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using YourNamespace.DTO;

namespace YourNamespace.Services
{
    public class AuthService
    {
        private readonly TokenService _tokenService;
        private readonly MongoDbService _mongoDbService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public AuthService(TokenService tokenService, MongoDbService mongoDbService, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, EmailService emailService)
        {
            _tokenService = tokenService;
            _mongoDbService = mongoDbService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _emailService = emailService;
        }

        private IMongoCollection<User> GetUsersCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<User>("Users");
        }

        private IMongoCollection<PasswordResetOtp> GetOtpCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<PasswordResetOtp>("PasswordResetOtps");
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
                OrganizationId = organizationId,
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
            try
            {
                var usersCollection = GetUsersCollection();
                var user = await usersCollection.Find(u => u.Email == login.Email).FirstOrDefaultAsync();

                if (user == null)
                {
                    return new UnauthorizedObjectResult(new { message = "Invalid email or password" });
                }

                if (!VerifyPassword(login.Password, user.PasswordHash))
                {
                    return new UnauthorizedObjectResult(new { message = "Invalid email or password" });
                }

                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();
                var hashedToken = HashToken(refreshToken);

                user.RefreshToken = hashedToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);

                SetAuthTokenCookie("LocalAccessToken", accessToken);
                SetAuthTokenCookie("LocalRefreshToken", refreshToken, false);

                // Fetch organization details
                var organizationsCollection = _mongoDbService.GetDatabase().GetCollection<BsonDocument>("OrganizationMst");
                var organizationObjectId = new ObjectId(user.OrganizationId);
                var organization = await organizationsCollection.Find(new BsonDocument("_id", organizationObjectId)).FirstOrDefaultAsync();

                return new OkObjectResult(new
                {
                    message = "Login successful",
                    userId = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    organizationId = user.OrganizationId,
                    roleids = user.RoleIds,
                    organization = organization != null ? new
                    {
                        id = organization["_id"].AsObjectId.ToString(),
                        name = organization["OrganizationName"].AsString,
                        status = organization["OrganizationStatus"].AsInt32,
                        type = organization["OrganizationType"].AsString,
                        code = organization["OrganizationCode"].AsString,
                        createdOn = organization["CreatedOn"].ToUniversalTime(),
                        updatedOn = organization["UpdatedOn"].ToUniversalTime(),
                        isDeleted = organization["IsDeleted"].AsBoolean
                    } : null
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error during login: {ex.Message}" }) { StatusCode = 500 };
            }
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
                var hashedToken = HashToken(refreshToken);
                var user = await usersCollection.Find(u => u.RefreshToken == hashedToken).FirstOrDefaultAsync();
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

            var usersCollection = GetUsersCollection();
            var user = await usersCollection.Find(u => u.Email == email).FirstOrDefaultAsync();

            if (user == null)
            {
                // Handle case where user doesn't exist in your system
                return new BadRequestObjectResult(new { message = "User not registered" });
            }

            var accessToken = _tokenService.GenerateAccessToken(user); // Pass user object instead of just email
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = HashToken(refreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);

            SetAuthTokenCookie("LocalAccessToken", accessToken);
            SetAuthTokenCookie("LocalRefreshToken", refreshToken, false);
            SetAuthTokenCookie("GoogleAccessToken", googleAccessToken, false);

            if (!string.IsNullOrEmpty(googleRefreshToken))
            {
                SetAuthTokenCookie("GoogleRefreshToken", googleRefreshToken, false);
            }

            return new OkObjectResult(new
            {
                message = "Google login successful",
                email,
                name
            });
        }

        private void SetAuthTokenCookie(string key, string value, bool httpOnly = true)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var context = _httpContextAccessor.HttpContext;
                var encodedValue = System.Web.HttpUtility.UrlEncode(value);
                context?.Response.Cookies.Append(key, encodedValue, new CookieOptions
                {
                    HttpOnly = httpOnly,
                    Secure = false,  // Set to false for HTTP localhost
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Path = "/",
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

        public async Task<IActionResult> GetUserPermissions()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return new BadRequestObjectResult(new { message = "Invalid request" });

            // Get token from cookie
            if (!context.Request.Cookies.TryGetValue("LocalAccessToken", out var token))
            {
                return new UnauthorizedObjectResult(new { message = "Unauthorized" });
            }

            try
            {
                // Get user ID from token
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return new UnauthorizedObjectResult(new { message = "Invalid token" });
                }

                // Get user from database
                var usersCollection = GetUsersCollection();
                var user = await usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();

                if (user == null)
                {
                    return new NotFoundObjectResult(new { message = "User not found" });
                }

                // Get user's roles
                var rolesCollection = _mongoDbService.GetDatabase().GetCollection<Role>("Roles");
                var roles = await rolesCollection.Find(r => user.RoleIds.Contains(r.Id) && r.IsActive).ToListAsync();

                // Get permissions from token
                var permissionsClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "permissions")?.Value;
                var permissions = permissionsClaim != null ? 
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(permissionsClaim) 
                    : new Dictionary<string, Dictionary<string, List<string>>>();

                return new OkObjectResult(new
                {
                    userId = user.Id,
                    email = user.Email,
                    roles = roles.Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.DisplayName,
                        r.Description
                    }),
                    permissions
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = $"Error processing token: {ex.Message}" });
            }
        }

        public async Task<IActionResult> RequestResetOtp(RequestResetOtpDto dto)
        {
            try
            {
                var usersCollection = GetUsersCollection();
                var user = await usersCollection.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
                if (user == null)
                    return new NotFoundObjectResult(new { message = "Email not found" });

                var otp = GenerateOtp();
                var expiry = DateTime.UtcNow.AddMinutes(10);
                var otpDoc = new PasswordResetOtp
                {
                    Email = dto.Email,
                    Otp = otp,
                    Expiry = expiry,
                    Used = false
                };
                var otpCollection = GetOtpCollection();
                await otpCollection.InsertOneAsync(otpDoc);

                await _emailService.SendEmailAsync(dto.Email, "Your Password Reset OTP", $"Your OTP is: {otp}. It is valid for 10 minutes.");
                return new OkObjectResult(new { message = "OTP sent to email" });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> VerifyResetOtp(VerifyResetOtpDto dto)
        {
            try
            {
                var otpCollection = GetOtpCollection();
                var otpDoc = await otpCollection.Find(o => o.Email == dto.Email && o.Otp == dto.Otp && !o.Used).FirstOrDefaultAsync();
                if (otpDoc == null)
                    return new BadRequestObjectResult(new { message = "Invalid OTP" });
                if (otpDoc.Expiry < DateTime.UtcNow)
                    return new BadRequestObjectResult(new { message = "OTP expired" });
                return new OkObjectResult(new { message = "OTP verified" });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            try
            {
                var otpCollection = GetOtpCollection();
                var otpDoc = await otpCollection.Find(o => o.Email == dto.Email && o.Otp == dto.Otp && !o.Used).FirstOrDefaultAsync();
                if (otpDoc == null)
                    return new BadRequestObjectResult(new { message = "Invalid OTP" });
                if (otpDoc.Expiry < DateTime.UtcNow)
                    return new BadRequestObjectResult(new { message = "OTP expired" });

                var usersCollection = GetUsersCollection();
                var user = await usersCollection.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
                if (user == null)
                    return new NotFoundObjectResult(new { message = "User not found" });

                user.PasswordHash = HashPassword(dto.NewPassword);
                user.UpdatedOn = DateTime.UtcNow;
                await usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);

                var update = Builders<PasswordResetOtp>.Update.Set(o => o.Used, true);
                await otpCollection.UpdateOneAsync(o => o.Id == otpDoc.Id, update);

                return new OkObjectResult(new { message = "Password reset successful" });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        private string GenerateOtp()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                return (BitConverter.ToUInt32(bytes, 0) % 1000000).ToString("D6");
            }
        }

        private static string HashToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return string.Empty;
            }

            // URL decode the token if it's encoded
            var decodedToken = System.Web.HttpUtility.UrlDecode(token);
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(decodedToken));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyToken(string token, string storedHash)
        {
            return HashToken(token) == storedHash;
        }

        public async Task<IActionResult> RefreshAccessToken(RefreshTokenRequestDto dto)
        {
            try 
            {
                if (string.IsNullOrEmpty(dto.RefreshToken))
                {
                    return new BadRequestObjectResult(new { message = "Refresh token is required" });
                }

                // URL decode the token if it's encoded
                var decodedToken = System.Web.HttpUtility.UrlDecode(dto.RefreshToken);
                var usersCollection = GetUsersCollection();
                var hashedToken = HashToken(decodedToken);

                var user = await usersCollection.Find(u => u.RefreshToken == hashedToken).FirstOrDefaultAsync();
                if (user == null)
                {
                    return new UnauthorizedObjectResult(new { message = "Invalid or expired refresh token" });
                }

                if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
                {
                    return new UnauthorizedObjectResult(new { message = "Invalid or expired refresh token" });
                }

                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                var newHashedToken = HashToken(newRefreshToken);

                user.RefreshToken = newHashedToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);

                SetAuthTokenCookie("LocalAccessToken", newAccessToken);
                SetAuthTokenCookie("LocalRefreshToken", newRefreshToken, false);

                return new OkObjectResult(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error refreshing token: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}