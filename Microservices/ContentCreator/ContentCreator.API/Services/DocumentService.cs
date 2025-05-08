using Library.Models;
using YourNamespace.Library.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Threading.Tasks;
using YourNamespace.DTO;

namespace YourNamespace.Services
{
    public class DocumentService
    {
        private readonly IMongoCollection<Document> _documents;
        private readonly GridFSBucket _gridFs;

        public DocumentService(MongoDbService mongoDbService)
        {
            var db = mongoDbService.GetDatabase();

            _gridFs = new GridFSBucket(db, new GridFSBucketOptions
            {
                BucketName = "documents",
                ChunkSizeBytes = 255 * 1024
            });

            _documents = db.GetCollection<Document>("Documents");
        }

        public async Task<IActionResult> UploadDocumentAsync(IFormFile file, string description)
        {
            if (file == null || file.Length == 0)
                return new BadRequestObjectResult(new { message = "File is required." });

            try
            {
                using var stream = file.OpenReadStream();

                var options = new GridFSUploadOptions
                {
                    Metadata = new BsonDocument
                    {
                        { "contentType", file.ContentType },
                        { "description", description }
                    }
                };

                var fileId = await _gridFs.UploadFromStreamAsync(file.FileName, stream, options);

                var document = new Document
                {
                    FileId = fileId,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Description = description,
                    UploadedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _documents.InsertOneAsync(document);

                return new OkObjectResult(new { message = "File uploaded successfully.", documentId = document.Id.ToString() });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> StreamDocumentAsync(string documentId, HttpResponse response)
        {
            if (!ObjectId.TryParse(documentId, out var objectId))
                return new BadRequestObjectResult(new { message = "Invalid document ID format." });

            var document = await _documents.Find(d => d.Id == objectId && !d.IsDeleted).FirstOrDefaultAsync();
            if (document == null)
                return new NotFoundObjectResult(new { message = "Document not found or has been deleted." });

            try
            {
                var stream = await _gridFs.OpenDownloadStreamAsync(document.FileId);
                var contentType = document.ContentType ?? "application/octet-stream";

                // Set the Content-Disposition header to enable inline viewing
                response.Headers.Append("Content-Disposition", new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline")
                {
                    FileName = document.FileName
                }.ToString());

                return new FileStreamResult(stream, contentType);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error streaming file: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> DeleteDocumentAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
                return new BadRequestObjectResult(new { message = "Invalid document ID." });

            var filter = Builders<Document>.Filter.Eq(d => d.Id, objectId);
            var update = Builders<Document>.Update
                .Set(d => d.IsDeleted, true)
                .Set(d => d.DeletedOn, DateTime.UtcNow);

            var result = await _documents.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
                return new NotFoundObjectResult(new { message = "Document not found." });

            return new OkObjectResult(new { message = "Document soft deleted successfully." });
        }

        public async Task<IActionResult> DownloadDocumentAsync(string documentId)
        {
            if (!ObjectId.TryParse(documentId, out var objectId))
                return new BadRequestObjectResult(new { message = "Invalid document ID format." });

            var document = await _documents
                .Find(d => d.Id == objectId && !d.IsDeleted)
                .FirstOrDefaultAsync();

            if (document == null)
                return new NotFoundObjectResult(new { message = "Document not found or deleted." });

            try
            {
                var stream = await _gridFs.OpenDownloadStreamAsync(document.FileId);

                // Create result first
                var result = new FileStreamResult(stream, document.ContentType ?? "application/octet-stream")
                {
                    FileDownloadName = document.FileName
                };

                // Add download header after response starts — Not possible in this way, so remove the line below
                // Response.Headers.Add("Content-Disposition", $"attachment; filename={document.FileName}");

                return result;
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error downloading file: {ex.Message}" }) { StatusCode = 500 };
            }
        }
        public async Task<IActionResult> GetAllGridFsMetadataAsync()
        {
            try
            {
                var database = _documents.Database;
                var filesCollection = database.GetCollection<BsonDocument>("documents.files");

                var files = await filesCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

                var metadataList = files.Select(f => new
                {
                    Id = f.GetValue("_id").ToString(),
                    Filename = f.GetValue("filename", "").AsString,
                    Length = f.GetValue("length", 0).ToInt64(),
                    ChunkSize = f.GetValue("chunkSize", 0).ToInt32(),
                    UploadDate = f.GetValue("uploadDate", BsonNull.Value).ToUniversalTime(),
                    Metadata = new
                    {
                        ContentType = f.GetValue("metadata")?.AsBsonDocument?.GetValue("contentType", "").AsString ?? "",
                        Description = f.GetValue("metadata")?.AsBsonDocument?.GetValue("description", "").AsString ?? ""
                    }
                });

                return new OkObjectResult(metadataList);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error fetching metadata: {ex.Message}" }) { StatusCode = 500 };
            }
        }

    }
}
