using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.Services;
using YourNamespace.Models;

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

    }
}
