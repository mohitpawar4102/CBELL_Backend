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
    [Route("api/features")]
    public class FeaturesController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public FeaturesController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFeatures([FromQuery] string moduleId = null)
        {
            var filter = moduleId != null
                ? Builders<Feature>.Filter.And(
                    Builders<Feature>.Filter.Eq(f => f.IsActive, true),
                    Builders<Feature>.Filter.Eq(f => f.ModuleId, moduleId))
                : Builders<Feature>.Filter.Eq(f => f.IsActive, true);

            var features = await _mongoDbService.GetDatabase()
                .GetCollection<Feature>("Features")
                .Find(filter)
                .ToListAsync();

            return Ok(features);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFeature([FromBody] FeatureDto featureDto)
        {
            try
            {
                // Verify module exists
                var moduleExists = await _mongoDbService.GetDatabase()
                    .GetCollection<Module>("Modules")
                    .Find(m => m.Id == featureDto.ModuleId && m.IsActive)
                    .AnyAsync();

                if (!moduleExists)
                    return BadRequest(new { message = "Module not found or inactive" });

                var feature = new Feature
                {
                    ModuleId = featureDto.ModuleId,
                    Name = featureDto.Name,
                    DisplayName = featureDto.DisplayName,
                    Description = featureDto.Description,
                    IsActive = true
                };

                await _mongoDbService.GetDatabase()
                    .GetCollection<Feature>("Features")
                    .InsertOneAsync(feature);

                return Ok(new
                {
                    message = "Feature created successfully",
                    feature
                });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}", stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeature(string id)
        {
            var feature = await _mongoDbService.GetDatabase()
                .GetCollection<Feature>("Features")
                .Find(f => f.Id == id && f.IsActive)
                .FirstOrDefaultAsync();

            if (feature == null)
                return NotFound(new { message = $"Feature with ID {id} not found" });

            return Ok(feature);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFeature(string id, [FromBody] FeatureDto featureDto)
        {
            // Verify module exists
            var moduleExists = await _mongoDbService.GetDatabase()
                .GetCollection<Module>("Modules")
                .Find(m => m.Id == featureDto.ModuleId && m.IsActive)
                .AnyAsync();

            if (!moduleExists)
                return BadRequest(new { message = "Module not found or inactive" });

            var feature = new Feature
            {
                Id = id,
                ModuleId = featureDto.ModuleId,
                Name = featureDto.Name,
                DisplayName = featureDto.DisplayName,
                Description = featureDto.Description,
                IsActive = true
            };

            var result = await _mongoDbService.GetDatabase()
                .GetCollection<Feature>("Features")
                .ReplaceOneAsync(f => f.Id == id, feature);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = $"Feature with ID {id} not found" });

            return Ok(feature);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeature(string id)
        {
            // Perform soft delete
            var update = Builders<Feature>.Update.Set(f => f.IsActive, false);
            var result = await _mongoDbService.GetDatabase()
                .GetCollection<Feature>("Features")
                .UpdateOneAsync(f => f.Id == id, update);

            if (result.ModifiedCount == 0)
                return NotFound(new { message = $"Feature with ID {id} not found" });

            return Ok(new { message = "Feature successfully deleted" });
        }
    }
}