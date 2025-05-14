using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.Services;
using YourNamespace.Models;
using YourNamespace.DTO;
using YourApiMicroservice.Auth; // Add this for AuthGuard
using Microsoft.AspNetCore.Authorization; // Add this for AllowAnonymous

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
        [AuthGuard("Tasks", "Task Management", "Create")]
        public Task<IActionResult> CreateTask([FromBody] TaskDTO taskDto) => _taskService.CreateTaskAsync(taskDto);

        [HttpGet("by-event/{eventId}")]
        [AuthGuard("Tasks", "TaskManagement", "Read")]
        public async Task<IActionResult> GetTasksByEventId(string eventId)
        {
            return await _taskService.GetTasksByEventIdAsync(eventId);
        }

        [HttpGet("get_all_tasks")]
        [AuthGuard("Tasks", "Task Management", "Read")]
        public Task<IActionResult> GetAllTasks() => _taskService.GetAllTasksAsync();

        [HttpGet("get_task/{id}")]
        [AuthGuard("Tasks", "Task Management", "Read")]
        public Task<IActionResult> GetTaskById(string id) => _taskService.GetTaskByIdAsync(id);

        [HttpPut("update/{id}")]
        [AuthGuard("Tasks", "Task Management", "Update")]
        public Task<IActionResult> UpdateTask(string id, [FromBody] TaskDTO taskDto) => _taskService.UpdateTaskAsync(id, taskDto);

        [HttpDelete("delete/{id}")]
        [AuthGuard("Tasks", "Task Management", "Delete")]
        public Task<IActionResult> DeleteTask(string id) => _taskService.SoftDeleteTaskAsync(id);

        [HttpPatch("restore/{id}")]
        [AuthGuard("Tasks", "Task Management", "Update")]
        public Task<IActionResult> RestoreTask(string id) => _taskService.RestoreTaskAsync(id);
    }
}