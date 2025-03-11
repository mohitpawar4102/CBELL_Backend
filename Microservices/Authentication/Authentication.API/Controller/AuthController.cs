using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using YourNamespace.Services;
using YourNamespace.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Google;

namespace YourNamespace.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel login)
        {
            var (success, message, token) = await _authService.LoginAsync(login);

            if (success)
            {
                return Ok(new { message, token });
            }

            return Unauthorized(new { message });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var (success, message) = _authService.Logout();
            return Ok(new { message });
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleCallback()
        {
            var (success, message, token, email, name) = await _authService.GoogleLoginAsync();

            if (success)
            {
                return Ok(new { message, token, email, name });
            }

            return BadRequest(new { message });
        }
    }
}
