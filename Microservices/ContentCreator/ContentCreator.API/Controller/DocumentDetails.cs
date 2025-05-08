using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DocumentDetailsController : ControllerBase
{
    private readonly DocumentDetailsService _service;

    public DocumentDetailsController(DocumentDetailsService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> AddDocumentDetail([FromBody] DocumentDetailsDto dto)
    {
        return await _service.AddDocumentDetailAsync(dto);
    }
    [HttpGet("task/{taskId}")]
    public async Task<IActionResult> GetDocumentMetadataByTaskId(string taskId)
    {
        try
        {
            var metadata = await _service.GetDocumentMetadataByTaskIdAsync(taskId);
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("event/{eventId}")]
    public async Task<IActionResult> GetDocumentMetadataByEventId(string eventId)
    {
        try
        {
            var metadata = await _service.GetDocumentMetadataByEventIdAsync(eventId);
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
