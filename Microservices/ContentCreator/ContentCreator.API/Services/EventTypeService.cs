using YourNamespace.Models;
using YourNamespace.DTO;
using YourNamespace.Library.Database;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YourNamespace.Services
{
    public class EventTypeService
    {
        private readonly MongoDbService _mongoDbService;

        // Constructor
        public EventTypeService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        // Dynamically accessing the collection like in TaskService
        private IMongoCollection<EventTypeModel> GetEventTypeCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<EventTypeModel>("EventTypes");
        }

        public async Task<IActionResult> GetAllEventTypesAsync()
        {
            try
            {
                var eventTypes = await GetEventTypeCollection().Find(_ => true).ToListAsync();

                // Converting the data to DTO for the response
                var eventTypeDtos = eventTypes.ConvertAll(e => new EventTypeDto
                {
                    Id = e.Id,
                    TypeName = e.TypeName,
                    TypeDescription = e.TypeDescription
                });

                return new OkObjectResult(eventTypeDtos);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> CreateEventTypeAsync(EventTypeDto eventTypeDto)
        {
            if (eventTypeDto == null || string.IsNullOrWhiteSpace(eventTypeDto.TypeName))
            {
                return new BadRequestObjectResult(new { message = "TypeName is required." });
            }

            var newEventType = new EventTypeModel
            {
                TypeName = eventTypeDto.TypeName,
                TypeDescription = eventTypeDto.TypeDescription
            };

            try
            {
                await GetEventTypeCollection().InsertOneAsync(newEventType);
                return new OkObjectResult(new { message = "Event type created successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
