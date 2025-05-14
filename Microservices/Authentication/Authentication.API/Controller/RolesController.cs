using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using YourNamespace.DTOs;
using YourNamespace.Services;
// using YourApiMicroservice.Auth; // Add this for AuthGuard if needed

namespace YourNamespace.Controllers
{
    [Route("api/roles")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly RoleService _roleService;

        public RolesController(RoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        // [AuthGuard("Administration", "Roles", "Read")] // Add if needed
        public Task<IActionResult> GetRoles() =>
            _roleService.GetRolesAsync();

        [HttpPost]
        // [AuthGuard("Administration", "Roles", "Create")] // Add if needed
        public Task<IActionResult> CreateRole([FromBody] RoleCreateDto roleDto) =>
            _roleService.CreateRoleAsync(roleDto);

        [HttpGet("{id}")]
        // [AuthGuard("Administration", "Roles", "Read")] // Add if needed
        public Task<IActionResult> GetRoleById(string id) =>
            _roleService.GetRoleByIdAsync(id);

        [HttpPut("{id}")]
        // [AuthGuard("Administration", "Roles", "Update")] // Add if needed
        public Task<IActionResult> UpdateRole(string id, [FromBody] RoleUpdateDto roleDto) =>
            _roleService.UpdateRoleAsync(id, roleDto);

        [HttpDelete("{id}")]
        // [AuthGuard("Administration", "Roles", "Delete")] // Add if needed
        public Task<IActionResult> DeleteRole(string id) =>
            _roleService.DeleteRoleAsync(id);

        [HttpPost("assign/{userId}")]
        // [AuthGuard("Administration", "Roles", "Update")] // Add if needed
        public Task<IActionResult> AssignRoleToUser(string userId, [FromBody] List<string> roleIds) =>
            _roleService.AssignRoleToUserAsync(userId, roleIds);

        [HttpPost("{roleId}/permissions")]
        // [AuthGuard("Administration", "Roles", "Update")] // Add if needed
        public Task<IActionResult> AddPermissionsToRole(string roleId, [FromBody] List<PermissionDto> permissions) =>
            _roleService.AddPermissionsToRoleAsync(roleId, permissions);
    }
}