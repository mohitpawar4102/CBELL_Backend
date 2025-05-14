using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using YourNamespace.Library.Database;
using YourNamespace.Models;
using YourNamespace.DTOs;

namespace YourNamespace.Services
{
    public class ModuleService
    {
        private readonly MongoDbService _mongoDbService;

        public ModuleService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        private IMongoCollection<Module> GetModulesCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<Module>("Modules");
        }

        public async Task<IActionResult> GetModulesAsync()
        {
            try
            {
                var modules = await GetModulesCollection()
                    .Find(m => m.IsActive)
                    .ToListAsync();
                    
                return new OkObjectResult(modules);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> CreateModuleAsync(ModuleDto moduleDto)
        {
            try
            {
                var module = new Module
                {
                    Name = moduleDto.Name,
                    DisplayName = moduleDto.DisplayName,
                    Description = moduleDto.Description,
                    IsActive = true
                };

                await GetModulesCollection().InsertOneAsync(module);
                    
                return new OkObjectResult(new 
                {
                    message = "Module created successfully",
                    module
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

        public async Task<IActionResult> GetModuleByIdAsync(string id)
        {
            try
            {
                var module = await GetModulesCollection()
                    .Find(m => m.Id == id && m.IsActive)
                    .FirstOrDefaultAsync();
                
                if (module == null)
                    return new NotFoundObjectResult(new { message = $"Module with ID {id} not found" });
                
                return new OkObjectResult(module);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> UpdateModuleAsync(string id, ModuleDto moduleDto)
        {
            try
            {
                var module = new Module
                {
                    Id = id,
                    Name = moduleDto.Name,
                    DisplayName = moduleDto.DisplayName,
                    Description = moduleDto.Description,
                    IsActive = true
                };

                var result = await GetModulesCollection()
                    .ReplaceOneAsync(m => m.Id == id, module);
                    
                if (result.ModifiedCount == 0)
                    return new NotFoundObjectResult(new { message = $"Module with ID {id} not found" });
                    
                return new OkObjectResult(new
                {
                    message = "Module updated successfully",
                    module
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

        public async Task<IActionResult> DeleteModuleAsync(string id)
        {
            try
            {
                // Perform soft delete
                var update = Builders<Module>.Update.Set(m => m.IsActive, false);
                var result = await GetModulesCollection()
                    .UpdateOneAsync(m => m.Id == id, update);
                    
                if (result.ModifiedCount == 0)
                    return new NotFoundObjectResult(new { message = $"Module with ID {id} not found" });
                    
                return new OkObjectResult(new { message = "Module successfully deleted" });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }
    }
}