using Microsoft.AspNetCore.Mvc;
using YourNamespace.DTO;
using YourNamespace.Services;
using System.Threading.Tasks;

namespace YourNamespace.Controller
{
    [ApiController]
    [Route("api/email")]
    public class EmailController : ControllerBase
    {
        private readonly EmailService _emailService;
        public EmailController(EmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromForm] EmailSendDto dto)
        {
            await _emailService.SendEmailAsync(dto);
            return Ok(new { message = "Email sent and record stored." });
        }
    }
} 