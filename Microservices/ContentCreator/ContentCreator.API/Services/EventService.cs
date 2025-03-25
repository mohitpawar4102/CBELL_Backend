using YourNamespace.Models;
using YourNamespace.Library.Database;
using YourNamespace.DTOs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YourNamespace.Services
{
    public class EventService
    {
        private readonly MongoDbService _mongoDbService;

        public EventService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
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
                Dignitaries = eventDto.Dignitaries,
                SpecialGuests = eventDto.SpecialGuests,
                CreatedBy = eventDto.CreatedBy,
                CreatedOn = DateTime.UtcNow,
                EventDate = eventDto.EventDate,
                UpdatedBy = eventDto.CreatedBy,
                UpdatedOn = DateTime.UtcNow
            };

            try
            {
                await _mongoDbService.Events.InsertOneAsync(newEvent);
                return new CreatedAtActionResult("CreateEvent", "Event", new { id = newEvent.Id }, newEvent);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetAllEventsAsync()
        {
            try
            {
                var events = await _mongoDbService.Events.Find(_ => true).ToListAsync();
                return new OkObjectResult(events);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetEventByIdAsync(string id)
        {
            try
            {
                var eventItem = await _mongoDbService.Events.Find(e => e.Id == id).FirstOrDefaultAsync();
                if (eventItem == null)
                    return new NotFoundObjectResult(new { message = "Event not found." });

                return new OkObjectResult(eventItem);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
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
                .Set(e => e.Dignitaries, eventDto.Dignitaries)
                .Set(e => e.SpecialGuests, eventDto.SpecialGuests)
                .Set(e => e.EventDate, eventDto.EventDate)
                .Set(e => e.UpdatedBy, eventDto.CreatedBy)
                .Set(e => e.UpdatedOn, DateTime.UtcNow);
                // .Set(e => e.OrganizationId, eventDto.OrganizationId);

            try
            {
                var result = await _mongoDbService.Events.UpdateOneAsync(e => e.Id == id, updateDefinition);
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
                var result = await _mongoDbService.Events.DeleteOneAsync(e => e.Id == id);
                if (result.DeletedCount == 0)
                    return new NotFoundObjectResult(new { message = "Event not found." });

                return new OkObjectResult(new { message = "Event deleted successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
