using Microsoft.AspNetCore.Mvc;
using YourNamespace.Services;
using YourNamespace.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace YourNamespace.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TokenService _tokenService;

        public AuthController(TokenService tokenService)
        {
            _tokenService = tokenService;

        }

        private static readonly Dictionary<string, string> users = new()
        {
            { "admin", "password123" },
            { "user", "mypassword" }
        };

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            if (login == null || string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
            {
                return BadRequest(new { message = "Invalid request data" });
            }

            if (users.TryGetValue(login.Username, out var storedPassword) && storedPassword == login.Password)
            {
                var token = _tokenService.GenerateToken(login.Username);
                return Ok(new { message = "Login successful", token });
            }

            return Unauthorized(new { message = "Invalid username or password" });
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            Console.WriteLine("TEST:" + nameof(GoogleResponse));
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleResponse), "Auth", null, Request.Scheme),
                AllowRefresh = true
            };
            properties.Items["LoginProvider"] = GoogleDefaults.AuthenticationScheme; // Explicitly setting provider

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }


        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {

            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                var errorDescription = HttpContext.Request.Query["error_description"];
                return Unauthorized(new { message = "Google authentication failed", error = errorDescription });
            }


            var claims = authenticateResult.Principal?.Identities?.FirstOrDefault()?.Claims;
            var userEmail = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized(new { message = "Google authentication failed" });

            var token = _tokenService.GenerateToken(userEmail);
            return Ok(new { message = "Google login successful", token });
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logout successful" });
        }
    }
}