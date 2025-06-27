using Microsoft.AspNetCore.Mvc;
using YourNamespace.DTOs;
using YourNamespace.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using YourApiMicroservice.Auth;
using System.Security.Claims; // Add this for AuthGuard

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/event")]
    public class EventController : ControllerBase
    {
        private readonly EventService _eventService;

        public EventController(EventService eventService)
        {
            _eventService = eventService;
        }

        [HttpPost("create_event")]
        [AuthGuard("Events", "Event Management", "Create")] // Requires Create permission
        public Task<IActionResult> CreateEvent([FromBody] EventDto eventDto) => _eventService.CreateEventAsync(eventDto);

        [HttpGet("get_all_events")]
        [AuthGuard("Events", "Event Management", "Read")] // Requires Read permission
        public Task<IActionResult> GetAllEvents([FromQuery] string organizationId) => _eventService.GetAllEventsAsync(organizationId);

        [HttpGet("get_event/{id}")]
        [AuthGuard("Events", "Event Management", "Read")] // Requires Read permission
        public Task<IActionResult> GetEventById(string id, [FromQuery] string organizationId) => _eventService.GetEventByIdAsync(id, organizationId);

        [HttpPut("update/{id}")]
        [AuthGuard("Events", "Event Management", "Update")] // Requires Update permission
        public Task<IActionResult> UpdateEvent(string id, [FromBody] EventDto eventDto) => _eventService.UpdateEventAsync(id, eventDto);

        [HttpDelete("delete/{id}")]
        [AuthGuard("Events", "Event Management", "Delete")] // Requires Delete permission
        public Task<IActionResult> DeleteEvent(string id) => _eventService.DeleteEventAsync(id);
    }
}