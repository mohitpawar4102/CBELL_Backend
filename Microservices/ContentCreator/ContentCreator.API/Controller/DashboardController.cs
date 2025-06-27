using Microsoft.AspNetCore.Mvc;
using YourApiMicroservice.Auth;
using YourNamespace.Services;
using MongoDB.Driver;
using YourNamespace.Models;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        [AuthGuard("Dashboard", "Dashboard Management", "Read")]
        public async Task<IActionResult> GetDashboard([FromQuery] string organizationId)
        {
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("organizationId is required.");

            return await _dashboardService.GetDashboardDataAsync(organizationId);
        }
    }
}
