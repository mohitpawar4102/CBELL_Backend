using YourNamespace.Models;
using YourNamespace.Library.Database;
using YourNamespace.DTOs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YourNamespace.DTO;
using YourNamespace.Library.Helpers;
using ModelChecklistItem = YourNamespace.Models.ChecklistItem;
using DtoChecklistItem = YourNamespace.DTO.ChecklistItem;
using Library.Models;

namespace YourNamespace.Services
{
    public class TaskService
    {
        private readonly MongoDbService _mongoDbService;

        public TaskService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        private IMongoCollection<TaskModel> GetTaskCollection()
        {
            return _mongoDbService.GetDatabase().GetCollection<TaskModel>("TasksMst");
        }

        public async Task<IActionResult> CreateTaskAsync(TaskDTO taskDto)
        {
            if (taskDto == null)
                return new BadRequestObjectResult(new { message = "Task data is required." });

            if (string.IsNullOrWhiteSpace(taskDto.TaskTitle))
                return new BadRequestObjectResult(new { message = "Task title is required." });

            if (taskDto.DueDate < DateTime.UtcNow)
                return new BadRequestObjectResult(new { message = "Due date cannot be in the past." });

            // For CreateTaskAsync
            var task = new TaskModel
            {
                EventId = taskDto.EventId,
                TaskTitle = taskDto.TaskTitle,
                TaskStatus = taskDto.TaskStatus,
                AssignedTo = taskDto.AssignedTo,
                CreatedBy = taskDto.CreatedBy,
                UpdatedBy = taskDto.CreatedBy,
                CreativeType = taskDto.CreativeType,
                DueDate = taskDto.DueDate,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                CreativeNumbers = taskDto.CreativeNumbers,
                ChecklistDetails = taskDto.ChecklistDetails?.Select(dto => new ModelChecklistItem
                {
                    Text = dto.Text,
                    Checked = dto.Checked,
                    IsPlaceholder = dto.IsPlaceholder
                }).ToList(),
                Description = taskDto.Description,
                OrganizationId = taskDto.OrganizationId
            };


            try
            {
                await GetTaskCollection().InsertOneAsync(task);
                return new OkObjectResult(new { message = "Task created successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetTasksByEventIdAsync(string eventId, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return new BadRequestObjectResult(new { message = "Event ID is required." });

            try
            {
                var filter = new BsonDocument
                {
                    { "EventId", eventId },
                    { "IsDeleted", false },
                    { "TaskStatus", new BsonDocument("$ne", "Published") }
                };

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    // Convert the userId to ObjectId for comparison
                    var userObjectId = new ObjectId(userId);
                    
                    // Add a match stage at the beginning of our aggregation pipeline to filter by userId
                    var pipeline = new List<BsonDocument>
                    {
                        new BsonDocument("$match", filter),
                        new BsonDocument("$addFields", new BsonDocument("AssignedToObjIds",
                            new BsonDocument("$map", new BsonDocument
                            {
                                { "input", "$AssignedTo" },
                                { "as", "userId" },
                                { "in", new BsonDocument("$toObjectId", "$$userId") }
                            }))),
                        new BsonDocument("$match", new BsonDocument("AssignedToObjIds",
                            new BsonDocument("$in", new BsonArray { userObjectId }))),
                        new BsonDocument("$lookup", new BsonDocument
                        {
                            { "from", "Users" },
                            { "localField", "AssignedToObjIds" },
                            { "foreignField", "_id" },
                            { "as", "AssignedUsers" }
                        }),
                        new BsonDocument("$addFields", new BsonDocument("AssignedToDetails",
                            new BsonDocument("$map", new BsonDocument
                            {
                                { "input", "$AssignedUsers" },
                                { "as", "user" },
                                { "in", new BsonDocument
                                    {
                                        { "Id", new BsonDocument("$toString", "$$user._id") },
                                        { "Name", new BsonDocument("$concat", new BsonArray { "$$user.FirstName", " ", "$$user.LastName" }) }
                                    }
                                }
                            }))),
                        new BsonDocument("$project", new BsonDocument
                        {
                            { "Id", "$_id" },
                            { "TaskTitle", 1 },
                            { "TaskStatus", 1 },
                            { "AssignedTo", "$AssignedToDetails" },
                            { "CreatedBy", 1 },
                            { "UpdatedBy", 1 },
                            { "CreativeType", 1 },
                            { "DueDate", 1 },
                            { "CreatedOn", 1 },
                            { "UpdatedOn", 1 },
                            { "Description", 1 },
                            { "OrganizationId", 1 },
                            { "EventId", 1 },
                            { "CreativeNumbers", 1 },
                            { "ChecklistDetails", 1 }
                        })
                    };

                    var db = _mongoDbService.GetDatabase();
                    var taskCollection = db.GetCollection<BsonDocument>("TasksMst");
                    var result = await taskCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                    if (result == null || result.Count == 0)
                        return new NotFoundObjectResult(new { message = "No tasks found for the given criteria." });

                    var tasks = result.Select(doc => new TaskWithUserDto
                    {
                        Id = doc.GetValue("Id").ToString(),
                        TaskTitle = doc.GetValue("TaskTitle", "").AsString,
                        TaskStatus = doc.GetValue("TaskStatus", "").AsString,
                        AssignedTo = doc.TryGetValue("AssignedTo", out var assignedToVal) && assignedToVal.IsBsonArray
                            ? assignedToVal.AsBsonArray.Select(x => new AssignedUserDto
                            {
                                Id = x["Id"].AsString,
                                Name = x["Name"].AsString
                            }).ToList()
                            : new List<AssignedUserDto>(),
                        CreatedBy = doc.GetValue("CreatedBy", 0).ToInt32(),
                        UpdatedBy = doc.GetValue("UpdatedBy", 0).ToInt32(),
                        CreativeType = doc.GetValue("CreativeType", "").AsString,
                        DueDate = doc.GetValue("DueDate").ToUniversalTime(),
                        CreatedOn = doc.GetValue("CreatedOn").ToUniversalTime(),
                        UpdatedOn = doc.GetValue("UpdatedOn").ToUniversalTime(),
                        Description = doc.GetValue("Description", "").AsString,
                        OrganizationId = doc.GetValue("OrganizationId", "").AsString,
                        EventId = doc.GetValue("EventId", "").AsString,
                        CreativeNumbers = doc.GetValue("CreativeNumbers", 0).ToInt32(),
                        ChecklistDetails = doc.Contains("ChecklistDetails") && doc["ChecklistDetails"].IsBsonArray
                            ? doc["ChecklistDetails"].AsBsonArray.Select(x => new DtoChecklistItem
                            {
                                Text = x["Text"].AsString,
                                Checked = x["Checked"].AsBoolean,
                                IsPlaceholder = x["IsPlaceholder"].AsBoolean
                            }).ToList()
                            : new List<DtoChecklistItem>()
                    }).ToList();

                    return new OkObjectResult(tasks);
                }
                else
                {
                    // If no userId provided, use the existing AggregateTasksAsync method
                    var tasks = await AggregateTasksAsync(filter);

                    if (tasks == null || tasks.Count == 0)
                        return new NotFoundObjectResult(new { message = "No tasks found for the given criteria." });

                    return new OkObjectResult(tasks);
                }
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetAllTasksAsync()
        {
            try
            {
                var filter = new BsonDocument
                {
                    { "IsDeleted", false },
                    { "TaskStatus", new BsonDocument("$ne", "Published") }
                };
                var tasks = await AggregateTasksAsync(filter);
                return new OkObjectResult(tasks);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetTaskByIdAsync(string id)
        {
            try
            {
                if (!ObjectId.TryParse(id, out ObjectId objectId))
                {
                    return new BadRequestObjectResult(new { message = "Invalid ID format." });
                }

                // Use a proper BsonDocument filter (not Filter<T>) for the aggregation pipeline
                var filter = new BsonDocument
        {
            { "_id", objectId },
            { "IsDeleted", false }
        };

                var tasks = await AggregateTasksAsync(filter);

                if (tasks == null || tasks.Count == 0)
                {
                    return new NotFoundObjectResult(new { message = "Task not found." });
                }

                return new OkObjectResult(tasks.First());
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }


        private async Task<List<TaskWithUserDto>> AggregateTasksAsync(BsonDocument? filter = null)
        {
            var db = _mongoDbService.GetDatabase();
            var taskCollection = db.GetCollection<BsonDocument>("TasksMst");

            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$match", filter ?? new BsonDocument("IsDeleted", false)),
                new BsonDocument("$addFields", new BsonDocument("AssignedToObjIds",
                    new BsonDocument("$map", new BsonDocument
                    {
                        { "input", "$AssignedTo" },
                        { "as", "userId" },
                        { "in", new BsonDocument("$toObjectId", "$$userId") }
                    }))),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Users" },
                    { "localField", "AssignedToObjIds" },
                    { "foreignField", "_id" },
                    { "as", "AssignedUsers" }
                }),
                new BsonDocument("$addFields", new BsonDocument("AssignedToDetails",
                    new BsonDocument("$map", new BsonDocument
                    {
                        { "input", "$AssignedUsers" },
                        { "as", "user" },
                        { "in", new BsonDocument
                            {
                                { "Id", new BsonDocument("$toString", "$$user._id") },
                                { "Name", new BsonDocument("$concat", new BsonArray { "$$user.FirstName", " ", "$$user.LastName" }) }
                            }
                        }
                    }))),
                new BsonDocument("$project", new BsonDocument
                {
                    { "Id", "$_id" },
                    { "TaskTitle", 1 },
                    { "TaskStatus", 1 },
                    { "AssignedTo", "$AssignedToDetails" },
                    { "CreatedBy", 1 },
                    { "UpdatedBy", 1 },
                    { "CreativeType", 1 },
                    { "DueDate", 1 },
                    { "CreatedOn", 1 },
                    { "UpdatedOn", 1 },
                    { "Description", 1 },
                    { "OrganizationId", 1 },
                    { "EventId", 1 },
                    { "CreativeNumbers", 1 },
                    { "ChecklistDetails", 1 }
                })
            };

            var result = await taskCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return result.Select(doc => new TaskWithUserDto
            {
                Id = doc.GetValue("Id").ToString(),
                TaskTitle = doc.GetValue("TaskTitle", "").AsString,
                TaskStatus = doc.GetValue("TaskStatus", "").AsString,
                AssignedTo = doc.TryGetValue("AssignedTo", out var assignedToVal) && assignedToVal.IsBsonArray
                    ? assignedToVal.AsBsonArray.Select(x => new AssignedUserDto
                    {
                        Id = x["Id"].AsString,
                        Name = x["Name"].AsString
                    }).ToList()
                    : new List<AssignedUserDto>(),
                CreatedBy = doc.GetValue("CreatedBy", 0).ToInt32(),
                UpdatedBy = doc.GetValue("UpdatedBy", 0).ToInt32(),
                CreativeType = doc.GetValue("CreativeType", "").AsString,
                DueDate = doc.GetValue("DueDate").ToUniversalTime(),
                CreatedOn = doc.GetValue("CreatedOn").ToUniversalTime(),
                UpdatedOn = doc.GetValue("UpdatedOn").ToUniversalTime(),
                Description = doc.GetValue("Description", "").AsString,
                OrganizationId = doc.GetValue("OrganizationId", "").AsString,
                EventId = doc.GetValue("EventId", "").AsString,
                CreativeNumbers = doc.GetValue("CreativeNumbers", 0).ToInt32(),
                ChecklistDetails = doc.Contains("ChecklistDetails") && doc["ChecklistDetails"].IsBsonArray
                    ? doc["ChecklistDetails"].AsBsonArray.Select(x => new DtoChecklistItem
                    {
                        Text = x["Text"].AsString,
                        Checked = x["Checked"].AsBoolean,
                        IsPlaceholder = x["IsPlaceholder"].AsBoolean
                    }).ToList()
                    : new List<DtoChecklistItem>()
            }).ToList();
        }


        public async Task<IActionResult> UpdateTaskAsync(string id, TaskDTO taskDto)
        {
            if (taskDto == null)
                return new BadRequestObjectResult(new { message = "Task data is required." });

            if (string.IsNullOrWhiteSpace(taskDto.TaskTitle))
                return new BadRequestObjectResult(new { message = "Task title is required." });

            if (taskDto.DueDate < DateTime.UtcNow)
                return new BadRequestObjectResult(new { message = "Due date cannot be in the past." });

            var updateDefinition = Builders<TaskModel>.Update
                .Set(t => t.TaskTitle, taskDto.TaskTitle)
                .Set(t => t.TaskStatus, taskDto.TaskStatus)
                .Set(t => t.AssignedTo, taskDto.AssignedTo)
                .Set(t => t.CreativeType, taskDto.CreativeType)
                .Set(t => t.DueDate, taskDto.DueDate)
                .Set(t => t.UpdatedBy, taskDto.UpdatedBy)
                .Set(t => t.UpdatedOn, DateTime.UtcNow)
                .Set(t => t.CreativeNumbers, taskDto.CreativeNumbers)
                .Set(t => t.ChecklistDetails, taskDto.ChecklistDetails?.Select(dto => new ModelChecklistItem
                {
                    Text = dto.Text,
                    Checked = dto.Checked,
                    IsPlaceholder = dto.IsPlaceholder
                }).ToList())
                .Set(t => t.Description, taskDto.Description);

            try
            {
                var result = await GetTaskCollection().UpdateOneAsync(t => t.Id == id, updateDefinition);
                if (result.MatchedCount == 0)
                    return new NotFoundObjectResult(new { message = "Task not found." });

                return new OkObjectResult(new { message = "Task updated successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> SoftDeleteTaskAsync(string id)
        {
            try
            {
                var filter = Builders<TaskModel>.Filter.Eq(t => t.Id, id);
                var update = Builders<TaskModel>.Update
                    .Set(t => t.IsDeleted, true)
                    .Set(t => t.DeletedOn, DateTime.UtcNow);

                var result = await GetTaskCollection().UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                    return new NotFoundObjectResult(new { message = "Task not found or already deleted." });

                return new OkObjectResult(new { message = "Task deleted successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> RestoreTaskAsync(string id)
        {
            try
            {
                var filter = Builders<TaskModel>.Filter.Eq(t => t.Id, id);
                var update = Builders<TaskModel>.Update
                    .Set(t => t.IsDeleted, false)
                    .Set(t => t.DeletedOn, (DateTime?)null);

                var result = await GetTaskCollection().UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                    return new NotFoundObjectResult(new { message = "Task not found or already active." });

                return new OkObjectResult(new { message = "Task restored successfully." });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        internal async Task<IActionResult> GetDashboardDataAsync()
        {
            throw new NotImplementedException();
        }

        // New method to fetch published tasks with their approved documents
        public async Task<IActionResult> GetPublishedTasksWithDocumentsAsync(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return new BadRequestObjectResult(new { message = "Event ID is required." });

            try
            {
                // 1. Fetch approved tasks for the event
                var filter = new BsonDocument
                {
                    { "IsDeleted", false },
                    { "TaskStatus", "Approved" },
                    { "EventId", eventId }
                };
                var tasks = await AggregateTasksAsync(filter);
                if (tasks == null || tasks.Count == 0)
                    return new NotFoundObjectResult(new { message = "No published tasks found for this event." });

                // Prepare Mongo collections
                var db = _mongoDbService.GetDatabase();
                var documentDetailsCollection = db.GetCollection<DocumentDetails>("DocumentDetails");
                var documentsCollection = db.GetCollection<Document>("Documents");

                var result = new List<YourNamespace.DTO.TaskWithUserAndDocumentsDto>();

                foreach (var task in tasks)
                {
                    // 2. Find DocumentDetails for this event and task
                    var docDetails = await documentDetailsCollection.Find(dd => dd.EventId == eventId && dd.TaskId == task.Id).ToListAsync();
                    var documentIds = docDetails.Select(dd => dd.DocumentId).Distinct().ToList();

                    // 3. Find Documents with Status: "Approved" or "Published" and IsDeleted: false
                    var approvedDocuments = new List<YourNamespace.DTO.DocumentWithMetadataDto>();
                    if (documentIds.Count > 0)
                    {
                        var filterDocs = Builders<Document>.Filter.In(d => d.Id, documentIds.Select(MongoDB.Bson.ObjectId.Parse)) &
                                         (Builders<Document>.Filter.Eq(d => d.Status, "Approved") |
                                          Builders<Document>.Filter.Eq(d => d.Status, "Published")) &
                                         Builders<Document>.Filter.Eq(d => d.IsDeleted, false);
                        var docs = await documentsCollection.Find(filterDocs).ToListAsync();
                        approvedDocuments = docs.Select(doc => new YourNamespace.DTO.DocumentWithMetadataDto
                        {
                            DocumentId = doc.Id.ToString(),
                            Filename = doc.FileName,
                            ContentType = doc.ContentType,
                            Description = doc.Description,
                            Status = doc.Status,
                            Id = doc.Id.ToString(),
                            PublishedTo = doc.PublishedTo,
                            FileId = doc.FileId != null ? doc.FileId.ToString() : null
                        }).ToList();
                    }

                    // 4. Compose the result DTO
                    var taskWithDocs = new YourNamespace.DTO.TaskWithUserAndDocumentsDto
                    {
                        Id = task.Id,
                        EventId = task.EventId,
                        TaskTitle = task.TaskTitle,
                        TaskStatus = task.TaskStatus,
                        AssignedTo = task.AssignedTo,
                        CreatedBy = task.CreatedBy,
                        UpdatedBy = task.UpdatedBy,
                        CreativeType = task.CreativeType,
                        DueDate = task.DueDate,
                        CreatedOn = task.CreatedOn,
                        UpdatedOn = task.UpdatedOn,
                        CreativeNumbers = task.CreativeNumbers,
                        ChecklistDetails = task.ChecklistDetails,
                        Description = task.Description,
                        OrganizationId = task.OrganizationId,
                        Documents = approvedDocuments
                    };
                    result.Add(taskWithDocs);
                }

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
