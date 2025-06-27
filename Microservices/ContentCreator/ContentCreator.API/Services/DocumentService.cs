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
        private readonly string _publicBaseUrl;

        public DocumentService(MongoDbService mongoDbService, IConfiguration config)
        {
            var db = mongoDbService.GetDatabase();

            _gridFs = new GridFSBucket(db, new GridFSBucketOptions
            {
                BucketName = "documents",
                ChunkSizeBytes = 255 * 1024
            });

            _documents = db.GetCollection<Document>("Documents");
            _publicBaseUrl = config["AppSettings:PublicBaseUrl"];
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
                var filesCollection = _documents.Database.GetCollection<BsonDocument>("documents.files");
                var documentsCollection = _documents.Database.GetCollection<BsonDocument>("Documents");

                var pipeline = new[]
                {
                    new BsonDocument("$lookup", new BsonDocument
                    {
                        { "from", "Documents" },
                        { "localField", "_id" },
                        { "foreignField", "fileId" },
                        { "as", "documentMeta" }
                    }),
                    new BsonDocument("$unwind", new BsonDocument
                    {
                        { "path", "$documentMeta" },
                        { "preserveNullAndEmptyArrays", false }
                    }),
                    new BsonDocument("$match", new BsonDocument("documentMeta.isDeleted", false)),
                    new BsonDocument("$project", new BsonDocument
                    {
                        { "_id", 0 },
                        { "id", "$_id" },
                        { "documentId", "$documentMeta._id" },
                        { "filename", "$filename" },
                        { "length", "$length" },
                        { "chunkSize", "$chunkSize" },
                        { "uploadDate", "$uploadDate" },
                        { "contentType", "$metadata.contentType" },
                        { "description", "$metadata.description" },
                        { "status", "$documentMeta.Status" },
                        { "publishedTo", "$documentMeta.PublishedTo" }
                    })
                };

                var results = await filesCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                var metadataList = results.Select(f =>
                {
                    var publishedTo = new List<object>();
                    if (f.Contains("publishedTo") && f["publishedTo"].IsBsonArray)
                    {
                        publishedTo = f["publishedTo"].AsBsonArray.Select(p => new
                        {
                            Platform = p["Platform"].AsString,
                            IsPublished = p["IsPublished"].AsBoolean,
                            PublishedById = p["PublishedById"].AsString,
                            PublishedByName = p["PublishedByName"].AsString,
                            PublishedAt = p["PublishedAt"].ToUniversalTime()
                        }).ToList<object>();
                    }

                    return new
                    {
                        Id = f["id"].AsObjectId.ToString(),
                        DocumentId = f["documentId"].AsObjectId.ToString(),
                        Filename = f["filename"].AsString,
                        Length = f["length"].AsInt64,
                        ChunkSize = f["chunkSize"].AsInt32,
                        UploadDate = f["uploadDate"].ToUniversalTime(),
                        ContentType = f["contentType"].AsString,
                        Description = f["description"].AsString,
                        Status = f["status"].AsString,
                        PublishedTo = publishedTo
                    };
                }).ToList();

                return new OkObjectResult(new
                {
                    data = metadataList,
                    totalCount = metadataList.Count
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new
                {
                    message = $"Error fetching metadata: {ex.Message}"
                })
                { StatusCode = 500 };
            }
        }


        public async Task<IActionResult> GenerateDocumentLinkAsync(string documentId)
        {
            if (!ObjectId.TryParse(documentId, out var objectId))
                return new BadRequestObjectResult(new { message = "Invalid document ID format." });

            try
            {
                var document = await _documents
                    .Find(d => d.Id == objectId && !d.IsDeleted)
                    .FirstOrDefaultAsync();

                if (document == null)
                    return new NotFoundObjectResult(new { message = "Document not found or has been deleted." });

                // Generate the direct access URL
                // Note: Replace "your-api-base-url" with your actual API base URL
                // ✅ Correct
                // var accessUrl = $"http://localhost:5000/api/document/view/{documentId}";
                // var baseUrl = "https://cbell.ai"; // ✅ must be HTTPS and public domain
                var accessUrl = $"{_publicBaseUrl}/apis/document/view/{documentId}";



                return new OkObjectResult(new
                {
                    message = "Document access link generated successfully.",
                    accessUrl = accessUrl,
                    documentName = document.FileName,
                    contentType = document.ContentType
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error generating document link: {ex.Message}" }) { StatusCode = 500 };
            }
        }
        public async Task<string> ApproveDocumentAsync(string documentId)
        {
            if (!ObjectId.TryParse(documentId, out var objectId))
                return "Invalid document ID.";

            var document = await _documents.Find(d => d.Id == objectId && !d.IsDeleted).FirstOrDefaultAsync();
            if (document == null)
                return "Document not found.";

            if (document.Status == "Approved")
                return "Document is already approved.";

            var filter = Builders<Document>.Filter.Eq(d => d.Id, objectId);
            var update = Builders<Document>.Update.Set(d => d.Status, "Approved");
            var result = await _documents.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0 ? "Document approved successfully." : "Approval failed.";
        }


        public async Task<string> AddClientPublishedRecordAsync(string documentId, List<string> platforms, string userId, string userName)
        {
            if (!ObjectId.TryParse(documentId, out var objectId))
                return "Invalid document ID.";

            var document = await _documents.Find(d => d.Id == objectId && !d.IsDeleted).FirstOrDefaultAsync();
            if (document == null)
                return "Document not found.";

            if (document.Status != "Approved")
                return "Document is not approved yet.";

            var alreadyPublished = new List<string>();
            var newlyAdded = new List<string>();

            foreach (var platform in platforms)
            {
                var existing = document.PublishedTo.FirstOrDefault(p => p.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));
                if (existing != null && existing.IsPublished)
                {
                    alreadyPublished.Add(platform);
                    continue;
                }

                document.PublishedTo.Add(new PublishStatus
                {
                    Platform = platform,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow,
                    PublishedById = userId,
                    PublishedByName = userName
                });

                newlyAdded.Add(platform);
            }

            if (newlyAdded.Any())
            {
                document.Status = "Published";
                await _documents.ReplaceOneAsync(d => d.Id == document.Id, document);
            }

            if (newlyAdded.Count == 0 && alreadyPublished.Count > 0)
                return $"Document is already published on: {string.Join(", ", alreadyPublished)}.";

            if (newlyAdded.Count > 0 && alreadyPublished.Count > 0)
                return $"Document published on: {string.Join(", ", newlyAdded)}. Already published on: {string.Join(", ", alreadyPublished)}.";

            return "Document published successfully.";
        }

        public async Task<IActionResult> ViewDocumentAsync(string documentId)
        {
            if (!ObjectId.TryParse(documentId, out var objectId))
                return new BadRequestObjectResult(new { message = "Invalid document ID format." });

            try
            {
                var document = await _documents
                    .Find(d => d.Id == objectId && !d.IsDeleted)
                    .FirstOrDefaultAsync();

                if (document == null)
                    return new NotFoundObjectResult(new { message = "Document not found or has been deleted." });

                var stream = await _gridFs.OpenDownloadStreamAsync(document.FileId);
                var contentType = document.ContentType ?? "application/octet-stream";

                // Set the Content-Disposition header to enable inline viewing
                return new FileStreamResult(stream, contentType)
                {
                    FileDownloadName = document.FileName,
                    EnableRangeProcessing = true // Enable partial content requests
                };
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"Error accessing document: {ex.Message}" }) { StatusCode = 500 };
            }
        }

    }
}
