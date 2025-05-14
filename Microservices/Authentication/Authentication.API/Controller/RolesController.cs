using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using YourNamespace.Library.Database;
using YourNamespace.Models;
using YourNamespace.DTOs; // Add this

namespace YourNamespace.Controllers
{
    [Route("api/roles")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public RolesController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var rolesCollection = _mongoDbService.GetDatabase().GetCollection<Role>("Roles");
            var roles = await rolesCollection.Find(r => r.IsActive).ToListAsync();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] RoleCreateDto roleDto)
        {
            var role = new Role
            {
                Name = roleDto.Name,
                DisplayName = roleDto.DisplayName,
                Description = roleDto.Description,
                IsActive = true,
                Permissions = new List<RolePermission>()
            };
            
            if (roleDto.Permissions != null && roleDto.Permissions.Count > 0)
            {
                foreach (var permDto in roleDto.Permissions)
                {
                    // Verify module exists
                    var moduleExists = await _mongoDbService.GetDatabase()
                        .GetCollection<Module>("Modules")
                        .Find(m => m.Id == permDto.ModuleId && m.IsActive)
                        .AnyAsync();
                    
                    if (!moduleExists)
                        return BadRequest(new { message = $"Module with ID {permDto.ModuleId} not found" });
                    
                    // Verify feature exists
                    var featureExists = await _mongoDbService.GetDatabase()
                        .GetCollection<Feature>("Features")
                        .Find(f => f.Id == permDto.FeatureId && f.IsActive)
                        .AnyAsync();
                    
                    if (!featureExists)
                        return BadRequest(new { message = $"Feature with ID {permDto.FeatureId} not found" });
                    
                    // Calculate permission value from the permission flags
                    var permissionTypes = await _mongoDbService.GetDatabase()
                        .GetCollection<PermissionType>("PermissionTypes")
                        .Find(pt => pt.IsActive)
                        .ToListAsync();
                    
                    int permissionValue = 0;
                    
                    if (permDto.PermissionFlags != null)
                    {
                        foreach (var flagDto in permDto.PermissionFlags)
                        {
                            // Verify permission type exists
                            var permType = permissionTypes.FirstOrDefault(pt => pt.Id == flagDto.PermissionTypeId);
                            if (permType == null)
                                return BadRequest(new { message = $"Permission type with ID {flagDto.PermissionTypeId} not found" });
                            
                            // Set bit if permission is granted
                            if (flagDto.IsGranted)
                            {
                                permissionValue |= (1 << permType.BitPosition);
                            }
                        }
                    }
                    
                    role.Permissions.Add(new RolePermission
                    {
                        ModuleId = permDto.ModuleId,
                        FeatureId = permDto.FeatureId,
                        PermissionValue = permissionValue
                    });
                }
            }
            
            var rolesCollection = _mongoDbService.GetDatabase().GetCollection<Role>("Roles");
            await rolesCollection.InsertOneAsync(role);
            
            return Ok(role);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleUpdateDto roleDto)
        {
            // Verify role exists
            var rolesCollection = _mongoDbService.GetDatabase().GetCollection<Role>("Roles");
            var existingRole = await rolesCollection.Find(r => r.Id == id).FirstOrDefaultAsync();
            
            if (existingRole == null)
                return NotFound(new { message = $"Role with ID {id} not found" });
                
            var role = new Role
            {
                Id = id,
                Name = roleDto.Name,
                DisplayName = roleDto.DisplayName,
                Description = roleDto.Description,
                IsActive = true,
                Permissions = new List<RolePermission>()
            };
            
            if (roleDto.Permissions != null && roleDto.Permissions.Count > 0)
            {
                foreach (var permDto in roleDto.Permissions)
                {
                    // Verify module exists
                    var moduleExists = await _mongoDbService.GetDatabase()
                        .GetCollection<Module>("Modules")
                        .Find(m => m.Id == permDto.ModuleId && m.IsActive)
                        .AnyAsync();
                    
                    if (!moduleExists)
                        return BadRequest(new { message = $"Module with ID {permDto.ModuleId} not found" });
                    
                    // Verify feature exists
                    var featureExists = await _mongoDbService.GetDatabase()
                        .GetCollection<Feature>("Features")
                        .Find(f => f.Id == permDto.FeatureId && f.IsActive)
                        .AnyAsync();
                    
                    if (!featureExists)
                        return BadRequest(new { message = $"Feature with ID {permDto.FeatureId} not found" });
                    
                    // Calculate permission value from the permission flags
                    var permissionTypes = await _mongoDbService.GetDatabase()
                        .GetCollection<PermissionType>("PermissionTypes")
                        .Find(pt => pt.IsActive)
                        .ToListAsync();
                    
                    int permissionValue = 0;
                    
                    if (permDto.PermissionFlags != null)
                    {
                        foreach (var flagDto in permDto.PermissionFlags)
                        {
                            // Verify permission type exists
                            var permType = permissionTypes.FirstOrDefault(pt => pt.Id == flagDto.PermissionTypeId);
                            if (permType == null)
                                return BadRequest(new { message = $"Permission type with ID {flagDto.PermissionTypeId} not found" });
                            
                            // Set bit if permission is granted
                            if (flagDto.IsGranted)
                            {
                                permissionValue |= (1 << permType.BitPosition);
                            }
                        }
                    }
                    
                    role.Permissions.Add(new RolePermission
                    {
                        ModuleId = permDto.ModuleId,
                        FeatureId = permDto.FeatureId,
                        PermissionValue = permissionValue
                    });
                }
            }
            
            await rolesCollection.ReplaceOneAsync(r => r.Id == id, role);
            return Ok(role);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            // Use soft delete instead of hard delete
            var update = Builders<Role>.Update.Set(r => r.IsActive, false);
            var result = await _mongoDbService.GetDatabase()
                .GetCollection<Role>("Roles")
                .UpdateOneAsync(r => r.Id == id, update);
                
            if (result.ModifiedCount == 0)
                return NotFound(new { message = $"Role with ID {id} not found" });
                
            return Ok(new { message = "Role successfully deleted" });
        }

        [HttpPost("assign/{userId}")]
        public async Task<IActionResult> AssignRoleToUser(string userId, [FromBody] List<string> roleIds)
        {
            var usersCollection = _mongoDbService.GetDatabase().GetCollection<User>("Users");
            var user = await usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            // Verify roles exist
            var rolesCollection = _mongoDbService.GetDatabase().GetCollection<Role>("Roles");
            foreach (var roleId in roleIds)
            {
                var roleExists = await rolesCollection.Find(r => r.Id == roleId && r.IsActive).AnyAsync();
                if (!roleExists)
                    return BadRequest(new { message = $"Role with ID {roleId} not found or inactive" });
            }
            
            user.RoleIds = roleIds;
            await usersCollection.ReplaceOneAsync(u => u.Id == userId, user);
            
            return Ok(new { message = "Roles assigned successfully", userId, roleIds });
        }
        
        // Get role by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var role = await _mongoDbService.GetDatabase()
                .GetCollection<Role>("Roles")
                .Find(r => r.Id == id && r.IsActive)
                .FirstOrDefaultAsync();
                
            if (role == null)
                return NotFound(new { message = $"Role with ID {id} not found" });
                
            return Ok(role);
        }
    }
}