using Microsoft.AspNetCore.Mvc;
using YourNamespace.DTOs;
using YourNamespace.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

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
        public Task<IActionResult> CreateEvent([FromBody] EventDto eventDto) => _eventService.CreateEventAsync(eventDto);

        [HttpGet("get_all_events")]
        public Task<IActionResult> GetAllEvents() => _eventService.GetAllEventsAsync();

        [HttpGet("get_event/{id}")]
        public Task<IActionResult> GetEventById(string id) => _eventService.GetEventByIdAsync(id);

        [HttpPut("update/{id}")]
        public Task<IActionResult> UpdateEvent(string id, [FromBody] EventDto eventDto) => _eventService.UpdateEventAsync(id, eventDto);

        [HttpDelete("delete/{id}")]
        public Task<IActionResult> DeleteEvent(string id) => _eventService.DeleteEventAsync(id);
    }
}
