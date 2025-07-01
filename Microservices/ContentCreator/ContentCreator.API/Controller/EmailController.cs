using Microsoft.AspNetCore.Mvc;
using YourNamespace.DTO;
using YourNamespace.Services;
using System.Threading.Tasks;
using Hangfire;

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
        public IActionResult SendEmail([FromForm] EmailSendDto dto)
        {
            BackgroundJob.Enqueue<EmailService>(service => service.SendEmailAsync(dto));
            return Ok(new { message = "Email sent Successfully." });
        }
    }
} 