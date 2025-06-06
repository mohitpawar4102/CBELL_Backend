using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.Services;
using YourNamespace.DTO;
using YourNamespace.DTOs;
using YourApiMicroservice.Auth;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/chat-thread")]
    public class ChatThreadController : ControllerBase
    {
        private readonly ChatThreadService _chatService;

        public ChatThreadController(ChatThreadService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("add-thread")]
        [AuthGuard("Thread", "Thread Management", "Create")] // Requires Create permission
        public async Task<IActionResult> AddThread([FromBody] CreateThreadDto dto)
        {
            return await _chatService.AddThreadToTaskChatAsync(dto);
        }

        [HttpGet("get-task-chat/{taskId}")]
        [AuthGuard("Thread", "Thread Management", "Read")]
        public async Task<IActionResult> GetTaskChat(string taskId)
        {
            return await _chatService.GetTaskChatByTaskIdAsync(taskId);
        }
    }
}