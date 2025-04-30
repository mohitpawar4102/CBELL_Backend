using YourNamespace.Models;
using YourNamespace.Library.Database;
using YourNamespace.DTOs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YourNamespace.DTO;
using YourNamespace.Library.Helpers;


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

        // 🔁 Reusable ID existence checker
        // private async Task<bool> IdExistsAsync(string collectionName, string id)
        // {
        //     if (!ObjectId.TryParse(id, out var objectId))
        //         return false;

        //     var collection = _mongoDbService.GetDatabase().GetCollection<BsonDocument>(collectionName);
        //     return await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", objectId)).AnyAsync();
        // }

        public async Task<IActionResult> CreateTaskAsync(TaskDTO taskDto)
        {
            if (taskDto == null)
                return new BadRequestObjectResult(new { message = "Task data is required." });

            if (string.IsNullOrWhiteSpace(taskDto.TaskTitle))
                return new BadRequestObjectResult(new { message = "Task title is required." });

            if (taskDto.DueDate < DateTime.UtcNow)
                return new BadRequestObjectResult(new { message = "Due date cannot be in the past." });

            // // ✅ Validate OrganizationId
            // if (string.IsNullOrWhiteSpace(taskDto.OrganizationId) ||
            //     !await IdExistsAsync("organizationmst", taskDto.OrganizationId))
            // {
            //     return new BadRequestObjectResult(new { message = "Invalid or non-existent OrganizationId." });
            // }

            // // ✅ Validate EventId
            // if (string.IsNullOrWhiteSpace(taskDto.EventId) ||
            //     !await IdExistsAsync("eventmst", taskDto.EventId))
            // {
            //     return new BadRequestObjectResult(new { message = "Invalid or non-existent EventId." });
            // }

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
                ChecklistDetails = taskDto.ChecklistDetails,
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

        public async Task<IActionResult> GetTasksByEventIdAsync(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return new BadRequestObjectResult(new { message = "Event ID is required." });

            try
            {
                var filter = Builders<TaskModel>.Filter.And(
                    Builders<TaskModel>.Filter.Eq(t => t.EventId, eventId),
                    Builders<TaskModel>.Filter.Eq(t => t.IsDeleted, false)
                );

                var tasks = await GetTaskCollection().Find(filter).ToListAsync();

                if (tasks == null || tasks.Count == 0)
                    return new NotFoundObjectResult(new { message = "No tasks found for the given Event ID." });

                return new OkObjectResult(tasks);
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
                var tasks = await GetTaskCollection().Find(t => !t.IsDeleted).ToListAsync();
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
                var task = await GetTaskCollection().Find(t => t.Id == id && !t.IsDeleted).FirstOrDefaultAsync();
                if (task == null)
                    return new NotFoundObjectResult(new { message = "Task not found." });

                return new OkObjectResult(task);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
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
                .Set(t => t.ChecklistDetails, taskDto.ChecklistDetails)
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
    }
}
