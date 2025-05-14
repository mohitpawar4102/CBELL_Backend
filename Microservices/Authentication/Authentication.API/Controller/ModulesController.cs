using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.DTOs;
using YourNamespace.Services;
// using YourApiMicroservice.Auth; // Add this for AuthGuard if needed

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/modules")]
    public class ModulesController : ControllerBase
    {
        private readonly ModuleService _moduleService;

        public ModulesController(ModuleService moduleService)
        {
            _moduleService = moduleService;
        }

        [HttpGet]
        // [AuthGuard("Administration", "Modules", "Read")] // Add if needed
        public Task<IActionResult> GetModules() => 
            _moduleService.GetModulesAsync();

        [HttpPost]
        // [AuthGuard("Administration", "Modules", "Create")] // Add if needed
        public Task<IActionResult> CreateModule([FromBody] ModuleDto moduleDto) => 
            _moduleService.CreateModuleAsync(moduleDto);

        [HttpGet("{id}")]
        // [AuthGuard("Administration", "Modules", "Read")] // Add if needed
        public Task<IActionResult> GetModule(string id) => 
            _moduleService.GetModuleByIdAsync(id);

        [HttpPut("{id}")]
        // [AuthGuard("Administration", "Modules", "Update")] // Add if needed
        public Task<IActionResult> UpdateModule(string id, [FromBody] ModuleDto moduleDto) => 
            _moduleService.UpdateModuleAsync(id, moduleDto);

        [HttpDelete("{id}")]
        // [AuthGuard("Administration", "Modules", "Delete")] // Add if needed
        public Task<IActionResult> DeleteModule(string id) => 
            _moduleService.DeleteModuleAsync(id);
    }
}