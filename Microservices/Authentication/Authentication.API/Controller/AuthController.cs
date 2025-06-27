using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.Services;
using YourNamespace.Models;
using YourNamespace.DTO;
using Microsoft.Extensions.Logging;

namespace YourNamespace.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public Task<IActionResult> Register([FromBody] RegisterRequest request) => _authService.Register(request);

        [HttpPost("login")]
        public Task<IActionResult> Login([FromBody] LoginModel login) => _authService.Login(login);

        [HttpGet("users")]
        public Task<IActionResult> GetAllUsers() => _authService.GetAllUsers();

        [HttpGet("google-login")]
        public IActionResult GoogleLogin() => _authService.InitiateGoogleLogin();

        [HttpGet("google-response")]
        public Task<IActionResult> GoogleCallback() => _authService.GoogleLoginAsync();

        [HttpPost("logout")]
        public Task<IActionResult> Logout() => _authService.Logout();

        [HttpGet("permissions")]
        public Task<IActionResult> GetUserPermissions() => _authService.GetUserPermissions();

        [HttpPost("request-reset-otp")]
        public Task<IActionResult> RequestResetOtp([FromBody] RequestResetOtpDto dto) => _authService.RequestResetOtp(dto);

        [HttpPost("verify-reset-otp")]
        public Task<IActionResult> VerifyResetOtp([FromBody] VerifyResetOtpDto dto) => _authService.VerifyResetOtp(dto);

        [HttpPost("reset-password")]
        public Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto) => _authService.ResetPassword(dto);

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshAccessToken([FromBody] RefreshTokenRequestDto dto)
        {
            _logger.LogInformation("Received refresh token request");
            
            // Check if refresh token is in cookies
            if (Request.Cookies.TryGetValue("LocalRefreshToken", out var cookieToken))
            {
                _logger.LogInformation("Found refresh token in cookies");
                // If no token provided in body but exists in cookie, use cookie token
                if (string.IsNullOrEmpty(dto.RefreshToken))
                {
                    _logger.LogInformation("Using token from cookie as request body is empty");
                    dto.RefreshToken = cookieToken;
                }
                else
                {
                    _logger.LogInformation("Token provided in both cookie and request body");
                }
            }
            else
            {
                _logger.LogInformation("No refresh token found in cookies");
            }

            if (string.IsNullOrEmpty(dto.RefreshToken))
            {
                _logger.LogWarning("No refresh token provided in either cookie or request body");
                return BadRequest(new { message = "Refresh token is required" });
            }

            // URL decode the token if needed
            dto.RefreshToken = System.Web.HttpUtility.UrlDecode(dto.RefreshToken);
            _logger.LogInformation($"Using refresh token (length: {dto.RefreshToken.Length})");

            return await _authService.RefreshAccessToken(dto);
        }
    }
}
