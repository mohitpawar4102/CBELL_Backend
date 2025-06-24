using System.Collections.Generic;
using System.Threading.Tasks;
using Library.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using YourNamespace.DTO;
using YourNamespace.Library.Database;

public class DocumentDetailsService
{
    private readonly IMongoCollection<DocumentDetails> _documentDetails;
    private readonly IMongoCollection<BsonDocument> _documents;
    private readonly IMongoCollection<BsonDocument> _files;

    public DocumentDetailsService(MongoDbService mongoDbService)
    {
        var db = mongoDbService.GetDatabase();
        _documentDetails = db.GetCollection<DocumentDetails>("DocumentDetails");
        _documents = db.GetCollection<BsonDocument>("Documents");
        _files = db.GetCollection<BsonDocument>("documents.files");
    }

    public async Task<IActionResult> AddDocumentDetailAsync(DocumentDetailsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DocumentId))
        {
            return new BadRequestObjectResult(new { message = "DocumentId is required." });
        }

        var documentDetail = new DocumentDetails
        {
            Id = ObjectId.GenerateNewId().ToString(),
            DocumentId = dto.DocumentId,
            OrganizationId = dto.OrganizationId,
            EventId = dto.EventId,
            TaskId = dto.TaskId,
            ConversationId = string.IsNullOrWhiteSpace(dto.ConversationId) ? null : dto.ConversationId,
            InsertedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        await _documentDetails.InsertOneAsync(documentDetail);

        return new OkObjectResult(new { message = "Document detail added successfully.", id = documentDetail.Id });
    }
    public async Task<List<object>> GetDocumentMetadataByTaskIdAsync(string taskId)
    {
        try
        {
            var taskObjectId = new ObjectId(taskId);

            var documentDetailsFilter = Builders<DocumentDetails>.Filter.Eq("TaskId", taskObjectId);
            var documentDetails = await _documentDetails.Find(documentDetailsFilter).ToListAsync();

            if (documentDetails.Count == 0)
            {
                throw new Exception("No documents found associated with the given TaskId.");
            }

            var documentIds = documentDetails.Select(dd => dd.DocumentId).ToList();

            var documentsFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.In("_id", documentIds.Select(ObjectId.Parse)),
                Builders<BsonDocument>.Filter.Eq("isDeleted", false)
            );
            var documents = await _documents.Find(documentsFilter).ToListAsync();

            if (documents.Count == 0)
            {
                throw new Exception("No documents found in the Documents collection for the given TaskId.");
            }

            var fileIdToDocumentMap = documents.ToDictionary(
                doc => doc["fileId"].AsObjectId,
                doc => doc
            );

            var fileIds = documents.Select(doc => doc["fileId"].AsObjectId).ToList();

            var filesFilter = Builders<BsonDocument>.Filter.In("_id", fileIds);
            var files = await _files.Find(filesFilter).ToListAsync();

            if (files.Count == 0)
            {
                throw new Exception("No files found for the given TaskId.");
            }

            var metadataList = files.Select(file => {
                var doc = fileIdToDocumentMap[file["_id"].AsObjectId];
                var publishedTo = new List<object>();
                if (doc.Contains("PublishedTo") && doc["PublishedTo"].IsBsonArray)
                {
                    publishedTo = doc["PublishedTo"].AsBsonArray.Select(p => new
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
                    DocumentId = doc["_id"].AsObjectId.ToString(),
                    Filename = file["filename"].AsString,
                    Length = file["length"].AsInt64,
                    ChunkSize = file["chunkSize"].AsInt32,
                    UploadDate = file["uploadDate"].ToUniversalTime(),
                    ContentType = file["metadata"]["contentType"].AsString,
                    Description = file["metadata"]["description"].AsString,
                    Status = doc.Contains("Status") ? doc["Status"].AsString : string.Empty,
                    PublishedTo = publishedTo
                };
            }).ToList<object>();

            return metadataList;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while fetching document metadata by TaskId: {ex.Message}");
        }
    }

    public async Task<List<object>> GetDocumentMetadataByEventIdAsync(string eventId)
    {
        try
        {
            var eventObjectId = new ObjectId(eventId);

            var documentDetailsFilter = Builders<DocumentDetails>.Filter.Eq("EventId", eventObjectId);
            var documentDetails = await _documentDetails.Find(documentDetailsFilter).ToListAsync();

            if (documentDetails.Count == 0)
            {
                throw new Exception("No documents found associated with the given EventId.");
            }

            var documentIds = documentDetails.Select(dd => dd.DocumentId).ToList();

            var documentsFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.In("_id", documentIds.Select(ObjectId.Parse)),
                Builders<BsonDocument>.Filter.Eq("isDeleted", false)
            );
            var documents = await _documents.Find(documentsFilter).ToListAsync();

            if (documents.Count == 0)
            {
                throw new Exception("No documents found in the Documents collection for the given EventId.");
            }

            var fileIdToDocumentMap = documents.ToDictionary(
                doc => doc["fileId"].AsObjectId,
                doc => doc
            );

            var fileIds = documents.Select(doc => doc["fileId"].AsObjectId).ToList();

            var filesFilter = Builders<BsonDocument>.Filter.In("_id", fileIds);
            var files = await _files.Find(filesFilter).ToListAsync();

            if (files.Count == 0)
            {
                throw new Exception("No files found in the documents.files collection for the given EventId.");
            }

            var metadataList = files.Select(file => {
                var doc = fileIdToDocumentMap[file["_id"].AsObjectId];
                var publishedTo = new List<object>();
                if (doc.Contains("PublishedTo") && doc["PublishedTo"].IsBsonArray)
                {
                    publishedTo = doc["PublishedTo"].AsBsonArray.Select(p => new
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
                    DocumentId = doc["_id"].AsObjectId.ToString(),
                    Filename = file["filename"].AsString,
                    Length = file["length"].AsInt64,
                    ChunkSize = file["chunkSize"].AsInt32,
                    UploadDate = file["uploadDate"].ToUniversalTime(),
                    ContentType = file["metadata"]["contentType"].AsString,
                    Description = file["metadata"]["description"].AsString,
                    Status = doc.Contains("Status") ? doc["Status"].AsString : string.Empty,
                    PublishedTo = publishedTo
                };
            }).ToList<object>();

            return metadataList;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while fetching document metadata by EventId: {ex.Message}");
        }
    }
}
