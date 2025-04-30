using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.Services;
using YourNamespace.DTO;

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
        public async Task<IActionResult> AddThread([FromBody] CreateThreadDto dto)
        {
            return await _chatService.AddThreadToTaskChatAsync(dto);
        }

        [HttpGet("get-task-chat/{taskId}")]
        public async Task<IActionResult> GetTaskChat(string taskId)
        {
            return await _chatService.GetTaskChatByTaskIdAsync(taskId);
        }
    }
}
