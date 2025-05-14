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
    [Route("api/modules")]
    public class ModulesController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public ModulesController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet]
        public async Task<IActionResult> GetModules()
        {
            var modules = await _mongoDbService.GetDatabase()
                .GetCollection<Module>("Modules")
                .Find(m => m.IsActive)
                .ToListAsync();
            return Ok(modules);
        }

        [HttpPost]
        public async Task<IActionResult> CreateModule([FromBody] ModuleDto moduleDto)
        {
            var module = new Module
            {
                Name = moduleDto.Name,
                DisplayName = moduleDto.DisplayName,
                Description = moduleDto.Description,
                IsActive = true
            };

            await _mongoDbService.GetDatabase()
                .GetCollection<Module>("Modules")
                .InsertOneAsync(module);
                
            return Ok(module);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetModule(string id)
        {
            var module = await _mongoDbService.GetDatabase()
                .GetCollection<Module>("Modules")
                .Find(m => m.Id == id && m.IsActive)
                .FirstOrDefaultAsync();
            
            if (module == null)
                return NotFound(new { message = $"Module with ID {id} not found" });
            
            return Ok(module);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModule(string id, [FromBody] ModuleDto moduleDto)
        {
            var module = new Module
            {
                Id = id,
                Name = moduleDto.Name,
                DisplayName = moduleDto.DisplayName,
                Description = moduleDto.Description,
                IsActive = true
            };

            var result = await _mongoDbService.GetDatabase()
                .GetCollection<Module>("Modules")
                .ReplaceOneAsync(m => m.Id == id, module);
                
            if (result.ModifiedCount == 0)
                return NotFound(new { message = $"Module with ID {id} not found" });
                
            return Ok(module);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModule(string id)
        {
            // Perform soft delete
            var update = Builders<Module>.Update.Set(m => m.IsActive, false);
            var result = await _mongoDbService.GetDatabase()
                .GetCollection<Module>("Modules")
                .UpdateOneAsync(m => m.Id == id, update);
                
            if (result.ModifiedCount == 0)
                return NotFound(new { message = $"Module with ID {id} not found" });
                
            return Ok(new { message = "Module successfully deleted" });
        }
    }
}