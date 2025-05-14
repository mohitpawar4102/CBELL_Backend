using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YourNamespace.Library.Database;
using YourNamespace.Models;
using YourNamespace.DTOs;

namespace YourNamespace.Services
{
    public class FeatureService
    {
        private readonly MongoDbService _mongoDbService;

        public FeatureService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        private IMongoCollection<Feature> GetFeaturesCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<Feature>("Features");
        }

        private IMongoCollection<Module> GetModulesCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<Module>("Modules");
        }

        public async Task<IActionResult> GetFeaturesAsync(string moduleId = null)
        {
            try
            {
                var filter = moduleId != null
                    ? Builders<Feature>.Filter.And(
                        Builders<Feature>.Filter.Eq(f => f.IsActive, true),
                        Builders<Feature>.Filter.Eq(f => f.ModuleId, moduleId))
                    : Builders<Feature>.Filter.Eq(f => f.IsActive, true);

                var features = await GetFeaturesCollection()
                    .Find(filter)
                    .ToListAsync();

                return new OkObjectResult(features);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> CreateFeatureAsync(FeatureDto featureDto)
        {
            try
            {
                // Verify module exists
                var moduleExists = await GetModulesCollection()
                    .Find(m => m.Id == featureDto.ModuleId && m.IsActive)
                    .AnyAsync();

                if (!moduleExists)
                    return new BadRequestObjectResult(new { message = "Module not found or inactive" });

                var feature = new Feature
                {
                    ModuleId = featureDto.ModuleId,
                    Name = featureDto.Name,
                    DisplayName = featureDto.DisplayName,
                    Description = featureDto.Description,
                    IsActive = true
                };

                await GetFeaturesCollection().InsertOneAsync(feature);

                return new OkObjectResult(new
                {
                    message = "Feature created successfully",
                    feature
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

        public async Task<IActionResult> GetFeatureByIdAsync(string id)
        {
            try
            {
                var feature = await GetFeaturesCollection()
                    .Find(f => f.Id == id && f.IsActive)
                    .FirstOrDefaultAsync();

                if (feature == null)
                    return new NotFoundObjectResult(new { message = $"Feature with ID {id} not found" });

                return new OkObjectResult(feature);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> UpdateFeatureAsync(string id, FeatureDto featureDto)
        {
            try
            {
                // Verify module exists
                var moduleExists = await GetModulesCollection()
                    .Find(m => m.Id == featureDto.ModuleId && m.IsActive)
                    .AnyAsync();

                if (!moduleExists)
                    return new BadRequestObjectResult(new { message = "Module not found or inactive" });

                var feature = new Feature
                {
                    Id = id,
                    ModuleId = featureDto.ModuleId,
                    Name = featureDto.Name,
                    DisplayName = featureDto.DisplayName,
                    Description = featureDto.Description,
                    IsActive = true
                };

                var result = await GetFeaturesCollection()
                    .ReplaceOneAsync(f => f.Id == id, feature);

                if (result.ModifiedCount == 0)
                    return new NotFoundObjectResult(new { message = $"Feature with ID {id} not found" });

                return new OkObjectResult(feature);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> DeleteFeatureAsync(string id)
        {
            try
            {
                // Perform soft delete
                var update = Builders<Feature>.Update.Set(f => f.IsActive, false);
                var result = await GetFeaturesCollection()
                    .UpdateOneAsync(f => f.Id == id, update);

                if (result.ModifiedCount == 0)
                    return new NotFoundObjectResult(new { message = $"Feature with ID {id} not found" });

                return new OkObjectResult(new { message = "Feature successfully deleted" });
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