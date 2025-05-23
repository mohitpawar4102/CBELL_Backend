using Microsoft.AspNetCore.Mvc;
using YourApiMicroservice.Auth;
using YourNamespace.Services;

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
        public async Task<IActionResult> GetDashboard()
        {
            return await _dashboardService.GetDashboardDataAsync();
        }
    }
}
