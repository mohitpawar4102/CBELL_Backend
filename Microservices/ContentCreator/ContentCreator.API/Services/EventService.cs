using YourNamespace.Models;
using YourNamespace.Library.Database;
using YourNamespace.DTOs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace YourNamespace.Services
{
    public class EventService
    {
        private readonly MongoDbService _mongoDbService;

        public EventService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }
        private IMongoCollection<Event> GetEventsCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<Event>("EventsMst");
        }

        private IMongoCollection<EventTypeModel> GetEventTypesCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<EventTypeModel>("EventTypes");
        }

        public async Task<IActionResult> CreateEventAsync(EventDto eventDto)
        {
            if (eventDto == null)
                return new BadRequestObjectResult(new { message = "Event data is required." });

            if (string.IsNullOrWhiteSpace(eventDto.EventName))
                return new BadRequestObjectResult(new { message = "Event name is required." });

            if (eventDto.EventDate < DateTime.UtcNow)
                return new BadRequestObjectResult(new { message = "Event date cannot be in the past." });

            var newEvent = new Event
            {
                EventName = eventDto.EventName,
                EventTypeId = eventDto.EventTypeId,
                EventTypeDesc = eventDto.EventTypeDesc,
                EventDescription = eventDto.EventDescription,
                LocationDetails = eventDto.LocationDetails,
                Coordinators = eventDto.Coordinators,
                SpecialGuests = eventDto.SpecialGuests,
                CreatedBy = eventDto.CreatedBy,
                CreatedOn = DateTime.UtcNow,
                EventDate = eventDto.EventDate,
                UpdatedBy = eventDto.CreatedBy,
                UpdatedOn = DateTime.UtcNow,
                OrganizationId = eventDto.OrganizationId
            };

            try
            {
                await GetEventsCollection().InsertOneAsync(newEvent);
                return new OkObjectResult(new { message = "Event created successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetAllEventsAsync(string organizationId)
        {
            if (string.IsNullOrWhiteSpace(organizationId))
                return new BadRequestObjectResult(new { message = "OrganizationId is required." });
            try
            {
                var events = await GetEventsCollection().Find(e => !e.IsDeleted && e.OrganizationId == organizationId).ToListAsync();
                if (events == null || !events.Any())
                    return new NotFoundObjectResult(new { message = "No events found for this organization." });

                // Fetch all event type ids
                var eventTypeIds = events.Select(e => e.EventTypeId).Distinct().ToList();
                var eventTypes = await GetEventTypesCollection().Find(et => eventTypeIds.Contains(et.Id)).ToListAsync();
                var eventTypeDict = eventTypes.ToDictionary(et => et.Id, et => et.TypeName);

                var result = events.Select(e => new {
                    e.Id,
                    e.EventName,
                    e.EventTypeId,
                    e.EventTypeDesc,
                    e.EventDescription,
                    e.LocationDetails,
                    e.Coordinators,
                    e.SpecialGuests,
                    e.CreatedBy,
                    e.CreatedOn,
                    e.EventDate,
                    e.UpdatedBy,
                    e.UpdatedOn,
                    e.OrganizationId,
                    e.IsDeleted,
                    e.DeletedOn,
                    TypeName = eventTypeDict.ContainsKey(e.EventTypeId) ? eventTypeDict[e.EventTypeId] : null
                });

                return new OkObjectResult(new {
                    message = "Events retrieved successfully.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                // Log the exception details here if you have logging implemented
                return new ObjectResult(new {
                    message = "An error occurred while retrieving events.",
                    error = ex.Message
                }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetEventByIdAsync(string id, string organizationId)
        {
            if (string.IsNullOrWhiteSpace(organizationId))
                return new BadRequestObjectResult(new { message = "OrganizationId is required." });
            if (string.IsNullOrWhiteSpace(id))
                return new BadRequestObjectResult(new { message = "Event ID is required." });
            try
            {
                var eventItem = await GetEventsCollection().Find(e => e.Id == id && !e.IsDeleted && e.OrganizationId == organizationId).FirstOrDefaultAsync();
                if (eventItem == null)
                    return new NotFoundObjectResult(new { message = "Event not found for this organization." });
                // Fetch the event type name
                string typeName = null;
                if (!string.IsNullOrEmpty(eventItem.EventTypeId))
                {
                    var eventType = await GetEventTypesCollection().Find(et => et.Id == eventItem.EventTypeId).FirstOrDefaultAsync();
                    typeName = eventType?.TypeName;
                }
                var result = new {
                    eventItem.Id,
                    eventItem.EventName,
                    eventItem.EventTypeId,
                    eventItem.EventTypeDesc,
                    eventItem.EventDescription,
                    eventItem.LocationDetails,
                    eventItem.Coordinators,
                    eventItem.SpecialGuests,
                    eventItem.CreatedBy,
                    eventItem.CreatedOn,
                    eventItem.EventDate,
                    eventItem.UpdatedBy,
                    eventItem.UpdatedOn,
                    eventItem.OrganizationId,
                    eventItem.IsDeleted,
                    eventItem.DeletedOn,
                    TypeName = typeName
                };
                return new OkObjectResult(new {
                    message = "Event retrieved successfully.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                // Log the exception details here if you have logging implemented
                return new ObjectResult(new {
                    message = "An error occurred while retrieving the event.",
                    error = ex.Message
                }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> UpdateEventAsync(string id, EventDto eventDto)
        {
            if (eventDto == null)
                return new BadRequestObjectResult(new { message = "Event data is required." });

            if (string.IsNullOrWhiteSpace(eventDto.EventName))
                return new BadRequestObjectResult(new { message = "Event name is required." });

            if (eventDto.EventDate < DateTime.UtcNow)
                return new BadRequestObjectResult(new { message = "Event date cannot be in the past." });

            var updateDefinition = Builders<Event>.Update
                .Set(e => e.EventName, eventDto.EventName)
                .Set(e => e.EventTypeId, eventDto.EventTypeId)
                .Set(e => e.EventTypeDesc, eventDto.EventTypeDesc)
                .Set(e => e.EventDescription, eventDto.EventDescription)
                .Set(e => e.LocationDetails, eventDto.LocationDetails)
                .Set(e => e.Coordinators, eventDto.Coordinators)
                .Set(e => e.SpecialGuests, eventDto.SpecialGuests)
                .Set(e => e.EventDate, eventDto.EventDate)
                .Set(e => e.UpdatedBy, eventDto.CreatedBy)
                .Set(e => e.UpdatedOn, DateTime.UtcNow);

            try
            {
                var result = await GetEventsCollection().UpdateOneAsync(e => e.Id == id, updateDefinition);
                if (result.MatchedCount == 0)
                    return new NotFoundObjectResult(new { message = "Event not found." });

                return new OkObjectResult(new { message = "Event updated successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> DeleteEventAsync(string id)
        {
            try
            {
                var updateDefinition = Builders<Event>.Update
                    .Set(e => e.IsDeleted, true)
                    .Set(e => e.DeletedOn, DateTime.UtcNow);

                var result = await GetEventsCollection().UpdateOneAsync(e => e.Id == id, updateDefinition);

                if (result.MatchedCount == 0)
                    return new NotFoundObjectResult(new { message = "Event not found." });

                return new OkObjectResult(new { message = "Event deleted successfully" });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
