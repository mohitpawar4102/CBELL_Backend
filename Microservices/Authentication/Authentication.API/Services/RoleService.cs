using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YourNamespace.Library.Database;
using YourNamespace.Models;
using YourNamespace.DTOs;

namespace YourNamespace.Services
{
    public class RoleService
    {
        private readonly MongoDbService _mongoDbService;

        public RoleService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        private IMongoCollection<Role> GetRolesCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<Role>("Roles");
        }

        private IMongoCollection<User> GetUsersCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<User>("Users");
        }

        private IMongoCollection<Module> GetModulesCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<Module>("Modules");
        }

        private IMongoCollection<Feature> GetFeaturesCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<Feature>("Features");
        }

        private IMongoCollection<PermissionType> GetPermissionTypesCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<PermissionType>("PermissionTypes");
        }

        public async Task<IActionResult> GetRolesAsync()
        {
            try
            {
                var roles = await GetRolesCollection()
                    .Find(r => r.IsActive)
                    .ToListAsync();

                return new OkObjectResult(roles);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> CreateRoleAsync(RoleCreateDto roleDto)
        {
            try
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
                    // Process permissions
                    var result = await ProcessPermissions(roleDto.Permissions);
                    if (result.Item1 != null) // Error occurred
                        return result.Item1;

                    role.Permissions = result.Item2;
                }

                await GetRolesCollection().InsertOneAsync(role);

                return new OkObjectResult(new
                {
                    message = "Role created successfully",
                    role
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> GetRoleByIdAsync(string id)
        {
            try
            {
                var role = await GetRolesCollection()
                    .Find(r => r.Id == id && r.IsActive)
                    .FirstOrDefaultAsync();

                if (role == null)
                    return new NotFoundObjectResult(new { message = $"Role with ID {id} not found" });

                return new OkObjectResult(role);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> UpdateRoleAsync(string id, RoleUpdateDto roleDto)
        {
            try
            {
                // Verify role exists
                var existingRole = await GetRolesCollection()
                    .Find(r => r.Id == id)
                    .FirstOrDefaultAsync();

                if (existingRole == null)
                    return new NotFoundObjectResult(new { message = $"Role with ID {id} not found" });

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
                    // Process permissions
                    var result = await ProcessPermissions(roleDto.Permissions);
                    if (result.Item1 != null) // Error occurred
                        return result.Item1;

                    role.Permissions = result.Item2;
                }

                await GetRolesCollection().ReplaceOneAsync(r => r.Id == id, role);

                return new OkObjectResult(new
                {
                    message = "Role updated successfully",
                    role
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> DeleteRoleAsync(string id)
        {
            try
            {
                // Use soft delete instead of hard delete
                var update = Builders<Role>.Update.Set(r => r.IsActive, false);
                var result = await GetRolesCollection()
                    .UpdateOneAsync(r => r.Id == id, update);

                if (result.ModifiedCount == 0)
                    return new NotFoundObjectResult(new { message = $"Role with ID {id} not found" });

                return new OkObjectResult(new { message = "Role successfully deleted" });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> AssignRoleToUserAsync(string userId, List<string> roleIds)
        {
            try
            {
                var user = await GetUsersCollection()
                    .Find(u => u.Id == userId)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return new NotFoundObjectResult(new { message = "User not found" });

                // Verify roles exist
                foreach (var roleId in roleIds)
                {
                    var roleExists = await GetRolesCollection()
                        .Find(r => r.Id == roleId && r.IsActive)
                        .AnyAsync();

                    if (!roleExists)
                        return new BadRequestObjectResult(new { message = $"Role with ID {roleId} not found or inactive" });
                }

                user.RoleIds = roleIds;
                await GetUsersCollection().ReplaceOneAsync(u => u.Id == userId, user);

                return new OkObjectResult(new
                {
                    message = "Roles assigned successfully",
                    userId,
                    roleIds
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }
        public async Task<IActionResult> AddPermissionsToRoleAsync(string roleId, List<PermissionDto> permissions)
        {
            try
            {
                // Verify role exists
                var role = await GetRolesCollection()
                    .Find(r => r.Id == roleId && r.IsActive)
                    .FirstOrDefaultAsync();

                if (role == null)
                    return new NotFoundObjectResult(new { message = $"Role with ID {roleId} not found" });

                // Process new permissions
                var result = await ProcessPermissions(permissions);
                if (result.Item1 != null) // Error occurred
                    return result.Item1;

                // Add new permissions to existing ones
                var existingPermissions = role.Permissions ?? new List<RolePermission>();

                // For each new permission, check if it already exists for the same module/feature
                foreach (var newPerm in result.Item2)
                {
                    var existingPerm = existingPermissions.FirstOrDefault(p =>
                        p.ModuleId == newPerm.ModuleId && p.FeatureId == newPerm.FeatureId);

                    if (existingPerm != null)
                    {
                        // Update existing permission
                        existingPerm.PermissionValue = newPerm.PermissionValue;
                    }
                    else
                    {
                        // Add new permission
                        existingPermissions.Add(newPerm);
                    }
                }

                // Update role with combined permissions
                var update = Builders<Role>.Update.Set(r => r.Permissions, existingPermissions);
                await GetRolesCollection().UpdateOneAsync(r => r.Id == roleId, update);

                return new OkObjectResult(new
                {
                    message = "Permissions added to role successfully",
                    roleId,
                    permissions = existingPermissions
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        // Helper method to process permissions and validate dependencies
        private async Task<Tuple<ObjectResult, List<RolePermission>>> ProcessPermissions(List<PermissionDto> permissionDtos)
        {
            var permissions = new List<RolePermission>();

            foreach (var permDto in permissionDtos)
            {
                // Verify module exists
                var moduleExists = await GetModulesCollection()
                    .Find(m => m.Id == permDto.ModuleId && m.IsActive)
                    .AnyAsync();

                if (!moduleExists)
                    return new Tuple<ObjectResult, List<RolePermission>>(
                        new BadRequestObjectResult(new { message = $"Module with ID {permDto.ModuleId} not found" }),
                        null
                    );

                // Verify feature exists
                var featureExists = await GetFeaturesCollection()
                    .Find(f => f.Id == permDto.FeatureId && f.IsActive)
                    .AnyAsync();

                if (!featureExists)
                    return new Tuple<ObjectResult, List<RolePermission>>(
                        new BadRequestObjectResult(new { message = $"Feature with ID {permDto.FeatureId} not found" }),
                        null
                    );

                // Calculate permission value from the permission flags
                var permissionTypes = await GetPermissionTypesCollection()
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
                            return new Tuple<ObjectResult, List<RolePermission>>(
                                new BadRequestObjectResult(new { message = $"Permission type with ID {flagDto.PermissionTypeId} not found" }),
                                null
                            );

                        // Set bit if permission is granted
                        if (flagDto.IsGranted)
                        {
                            permissionValue |= (1 << permType.BitPosition);
                        }
                    }
                }

                permissions.Add(new RolePermission
                {
                    ModuleId = permDto.ModuleId,
                    FeatureId = permDto.FeatureId,
                    PermissionValue = permissionValue
                });
            }

            return new Tuple<ObjectResult, List<RolePermission>>(null, permissions);
        }
    }
}