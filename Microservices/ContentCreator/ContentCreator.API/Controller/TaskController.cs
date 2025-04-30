using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.Services;
using YourNamespace.Models;
using YourNamespace.DTO;

namespace YourNamespace.Controllers
{
    [Route("api/task")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly TaskService _taskService;

        public TaskController(TaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpPost("create_task")]
        public Task<IActionResult> CreateTask([FromBody] TaskDTO taskDto) => _taskService.CreateTaskAsync(taskDto);

        [HttpGet("by-event/{eventId}")]
        public async Task<IActionResult> GetTasksByEventId(string eventId)
        {
            return await _taskService.GetTasksByEventIdAsync(eventId);
        }

        [HttpGet("get_all_tasks")]
        public Task<IActionResult> GetAllTasks() => _taskService.GetAllTasksAsync();

        [HttpGet("get_task/{id}")]
        public Task<IActionResult> GetTaskById(string id) => _taskService.GetTaskByIdAsync(id);

        [HttpPut("update/{id}")]
        public Task<IActionResult> UpdateTask(string id, [FromBody] TaskDTO taskDto) => _taskService.UpdateTaskAsync(id, taskDto);

        [HttpDelete("delete/{id}")]
        public Task<IActionResult> DeleteTask(string id) => _taskService.SoftDeleteTaskAsync(id);

        [HttpPatch("restore/{id}")]
        public Task<IActionResult> RestoreTask(string id) => _taskService.RestoreTaskAsync(id);
    }
}
