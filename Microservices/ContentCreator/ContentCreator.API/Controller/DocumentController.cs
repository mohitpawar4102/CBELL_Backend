using Microsoft.AspNetCore.Mvc;
using YourNamespace.DTOs;
using YourNamespace.Services;
using System.Threading.Tasks;
using YourApiMicroservice.Auth;

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
        [AuthGuard("Document", "Document Management", "Create")]
        public Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto dto) =>
            _documentService.UploadDocumentAsync(dto.File, dto.Description);

        [HttpGet("view/{id}")]
        [AuthGuard("Document", "Document Management", "Read")]
        public Task<IActionResult> ViewDocument(string id) =>
        _documentService.StreamDocumentAsync(id, Response);

        [HttpDelete("delete/{id}")]
        [AuthGuard("Document", "Document Management", "Delete")]
        public Task<IActionResult> DeleteDocument(string id) =>
            _documentService.DeleteDocumentAsync(id);

        [HttpGet("download/{documentId}")]
        [AuthGuard("Document", "Document Management", "Read")]
        public Task<IActionResult> DownloadDocument(string documentId)
        {
            return _documentService.DownloadDocumentAsync(documentId);
        }
        [HttpGet("metadata")]
        [AuthGuard("Document", "Document Management", "Read")]
        public async Task<IActionResult> GetAllGridFsMetadata()
        {
            return await _documentService.GetAllGridFsMetadataAsync();
        }
    }
}
