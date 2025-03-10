using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YourNamespace.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpGet("profile")]
        [Authorize] // This ensures only authenticated users can access this endpoint
        public IActionResult GetUserProfile()
        {
            // Console.WriteLine("Testing");
            return Ok(new
            {
                message = "This is a secured profile",
                // user = User!.Identity!.Name 
            });
        }
    }
}
