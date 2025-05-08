using YourNamespace.Models;
using YourNamespace.Library.Database;
using YourNamespace.DTOs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Library.Models;

namespace YourNamespace.Services
{
    public class OrganizationService
    {
        private readonly MongoDbService _mongoDbService;

        public OrganizationService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        private IMongoCollection<OrganizationModel> GetOrganizationCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<OrganizationModel>("OrganizationMst");
        }

        public async Task<IActionResult> CreateOrganizationAsync(OrganizationDTO organizationDto)
        {
            if (organizationDto == null)
                return new BadRequestObjectResult(new { message = "Organization data is required." });

            if (string.IsNullOrWhiteSpace(organizationDto.OrganizationName))
                return new BadRequestObjectResult(new { message = "Organization name is required." });

            // Check for duplicate OrganizationCode
            var existingOrgWithCode = await GetOrganizationCollection()
                .Find(o => o.OrganizationCode == organizationDto.OrganizationCode && o.IsDeleted == false)
                .FirstOrDefaultAsync();

            if (existingOrgWithCode != null)
                return new ConflictObjectResult(new { message = "Organization code already exists." });

            var organization = new OrganizationModel
            {
                OrganizationName = organizationDto.OrganizationName,
                OrganizationStatus = organizationDto.OrganizationStatus,
                OrganizationType = organizationDto.OrganizationType,
                OrganizationCode = organizationDto.OrganizationCode,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                IsDeleted = false
            };

            try
            {
                await GetOrganizationCollection().InsertOneAsync(organization);
                return new OkObjectResult(new { message = "Organization created successfully.", organizationId = organization.Id });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }


        public async Task<IActionResult> GetOrganizationByIdAsync(string id)
        {
            try
            {
                var organization = await GetOrganizationCollection()
                    .Find(o => o.Id == id && o.IsDeleted == false) // Check if IsDeleted is false
                    .FirstOrDefaultAsync();

                if (organization == null)
                    return new NotFoundObjectResult(new { message = "Organization not found." });

                return new OkObjectResult(organization);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetAllOrganizationsAsync()
        {
            try
            {
                var organizations = await GetOrganizationCollection()
                    .Find(o => o.IsDeleted == false) // Filter to get only non-deleted organizations
                    .ToListAsync();

                return new OkObjectResult(organizations);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> UpdateOrganizationAsync(string id, OrganizationDTO organizationDto)
        {
            // Console.WriteLine($"Received ID: {id}");
            // Console.WriteLine($"Received DTO: {System.Text.Json.JsonSerializer.Serialize(organizationDto)}");

            if (organizationDto == null)
                return new BadRequestObjectResult(new { message = "Organization data is required." });

            // Check if OrganizationCode is already used by another organization
            var existingOrgWithCode = await GetOrganizationCollection()
                .Find(o => o.OrganizationCode == organizationDto.OrganizationCode && o.Id != id && o.IsDeleted == false)
                .FirstOrDefaultAsync();

            if (existingOrgWithCode != null)
                return new ConflictObjectResult(new { message = "Organization code already exists for another organization." });


            var updateDefinition = Builders<OrganizationModel>.Update
                .Set(o => o.OrganizationName, organizationDto.OrganizationName)
                .Set(o => o.OrganizationStatus, organizationDto.OrganizationStatus)
                .Set(o => o.OrganizationType, organizationDto.OrganizationType)
                .Set(o => o.UpdatedOn, DateTime.UtcNow)
                .Set(o => o.OrganizationCode, organizationDto.OrganizationCode);
            try
            {
                // Check if the organization is not deleted before updating
                var result = await GetOrganizationCollection().UpdateOneAsync(
                    o => o.Id == id && o.IsDeleted == false, updateDefinition);  // Match using string Id and IsDeleted

                if (result.MatchedCount == 0)
                    return new NotFoundObjectResult(new { message = "Organization not found or already deleted." });

                return new OkObjectResult(new { message = "Organization updated successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> SoftDeleteOrganizationAsync(string id)
        {
            try
            {
                var filter = Builders<OrganizationModel>.Filter.Eq(o => o.Id, id) & Builders<OrganizationModel>.Filter.Eq(o => o.IsDeleted, false); // Use string Id and check if it's not already deleted
                var update = Builders<OrganizationModel>.Update
                    .Set(o => o.IsDeleted, true)
                    .Set(o => o.DeletedOn, DateTime.UtcNow);

                var result = await GetOrganizationCollection().UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                    return new NotFoundObjectResult(new { message = "Organization not found or already deleted." });

                return new OkObjectResult(new { message = "Organization deleted successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
