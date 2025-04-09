using Microsoft.AspNetCore.Mvc;
using YourNamespace.DTOs;
using YourNamespace.Services;
using System.Threading.Tasks;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/document")]
    public class DocumentController : ControllerBase
    {
        private readonly DocumentService _documentService;

        public DocumentController(DocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpPost("upload_document")]
        public Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto dto) =>
            _documentService.UploadDocumentAsync(dto.File, dto.Description);

        [HttpGet("view/{id}")]
        public Task<IActionResult> ViewDocument(string id) =>
            _documentService.StreamDocumentAsync(id);

        [HttpDelete("delete/{id}")]
        public Task<IActionResult> DeleteDocument(string id) =>
            _documentService.DeleteDocumentAsync(id);

        [HttpGet("download/{documentId}")]
        public Task<IActionResult> DownloadDocument(string documentId)
        {
            return _documentService.DownloadDocumentAsync(documentId);
        }

    }
}
