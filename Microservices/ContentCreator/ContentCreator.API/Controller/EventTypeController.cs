using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.Services;
using YourNamespace.DTO;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/eventtype")]
    public class EventTypeController : ControllerBase
    {
        private readonly EventTypeService _eventTypeService;
        public EventTypeController(EventTypeService eventTypeService)
        {
            _eventTypeService = eventTypeService;
        }

        [HttpGet("get_all_event-types")]
        public Task<IActionResult> GetEventTypes() => _eventTypeService.GetAllEventTypesAsync();

        [HttpPost("create")]
        public Task<IActionResult> CreateEventType([FromBody] EventTypeDto eventTypeDto) => _eventTypeService.CreateEventTypeAsync(eventTypeDto);
    }
}
