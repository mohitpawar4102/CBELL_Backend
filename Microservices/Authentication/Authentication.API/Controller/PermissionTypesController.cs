using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using YourNamespace.Library.Database;
using YourNamespace.Models;
using YourNamespace.DTOs;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/permission-types")]
    public class PermissionTypesController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public PermissionTypesController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPermissionTypes()
        {
            var permissionTypes = await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .Find(pt => pt.IsActive)
                .ToListAsync();
            return Ok(permissionTypes);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePermissionType([FromBody] PermissionTypeDto permissionTypeDto)
        {
            // Check if name already exists
            var existingWithName = await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .Find(pt => pt.Name == permissionTypeDto.Name && pt.IsActive)
                .FirstOrDefaultAsync();
                
            if (existingWithName != null)
                return BadRequest(new { message = $"A permission type with name '{permissionTypeDto.Name}' already exists" });
            
            // Validate bit position is unique
            var existingWithBitPosition = await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .Find(pt => pt.BitPosition == permissionTypeDto.BitPosition && pt.IsActive)
                .FirstOrDefaultAsync();
                
            if (existingWithBitPosition != null)
                return BadRequest(new { message = $"A permission type with bit position {permissionTypeDto.BitPosition} already exists" });
            
            var permissionType = new PermissionType
            {
                Name = permissionTypeDto.Name,
                DisplayName = permissionTypeDto.DisplayName,
                BitPosition = permissionTypeDto.BitPosition,
                IsActive = true
            };
            
            await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .InsertOneAsync(permissionType);
                
            return Ok(permissionType);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPermissionType(string id)
        {
            var permissionType = await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .Find(pt => pt.Id == id && pt.IsActive)
                .FirstOrDefaultAsync();
            
            if (permissionType == null)
                return NotFound(new { message = $"Permission type with ID {id} not found" });
            
            return Ok(permissionType);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermissionType(string id, [FromBody] PermissionTypeDto permissionTypeDto)
        {
            // Check if name already exists (except for this record)
            var existingWithName = await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .Find(pt => pt.Name == permissionTypeDto.Name && pt.Id != id && pt.IsActive)
                .FirstOrDefaultAsync();
                
            if (existingWithName != null)
                return BadRequest(new { message = $"A permission type with name '{permissionTypeDto.Name}' already exists" });
            
            // Validate bit position is unique (except for this record)
            var existingWithBitPosition = await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .Find(pt => pt.BitPosition == permissionTypeDto.BitPosition && pt.Id != id && pt.IsActive)
                .FirstOrDefaultAsync();
                
            if (existingWithBitPosition != null)
                return BadRequest(new { message = $"A permission type with bit position {permissionTypeDto.BitPosition} already exists" });
            
            var permissionType = new PermissionType
            {
                Id = id,
                Name = permissionTypeDto.Name,
                DisplayName = permissionTypeDto.DisplayName,
                BitPosition = permissionTypeDto.BitPosition,
                IsActive = true
            };
            
            var result = await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .ReplaceOneAsync(pt => pt.Id == id, permissionType);
                
            if (result.ModifiedCount == 0)
                return NotFound(new { message = $"Permission type with ID {id} not found" });
                
            return Ok(permissionType);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermissionType(string id)
        {
            // Perform soft delete
            var update = Builders<PermissionType>.Update.Set(pt => pt.IsActive, false);
            var result = await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .UpdateOneAsync(pt => pt.Id == id, update);
                
            if (result.ModifiedCount == 0)
                return NotFound(new { message = $"Permission type with ID {id} not found" });
                
            return Ok(new { message = "Permission type successfully deleted" });
        }

        [HttpPost("setup-defaults")]
        public async Task<IActionResult> SetupDefaultPermissionTypes()
        {
            // Check if default permissions already exist
            var existingCount = await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .CountDocumentsAsync(pt => pt.IsActive);
                
            if (existingCount > 0)
            {
                return Ok(new { message = "Permission types already exist", count = existingCount });
            }

            // Default permissions with sequential bit positions
            var permissionTypes = new List<PermissionType>
            {
                new PermissionType { Name = "Create", DisplayName = "Create", BitPosition = 0, IsActive = true },
                new PermissionType { Name = "Read", DisplayName = "Read", BitPosition = 1, IsActive = true },
                new PermissionType { Name = "Update", DisplayName = "Update", BitPosition = 2, IsActive = true },
                new PermissionType { Name = "Delete", DisplayName = "Delete", BitPosition = 3, IsActive = true }
            };

            await _mongoDbService.GetDatabase()
                .GetCollection<PermissionType>("PermissionTypes")
                .InsertManyAsync(permissionTypes);

            return Ok(new { message = "Default permission types created", permissionTypes });
        }
    }
}