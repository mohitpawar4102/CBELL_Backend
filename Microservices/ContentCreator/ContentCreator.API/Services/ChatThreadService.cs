using MongoDB.Driver;
using YourNamespace.Models;
using YourNamespace.Library.Database;
using YourNamespace.DTO;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YourNamespace.DTOs;
using System.Xml;

namespace YourNamespace.Services
{
    public class ChatThreadService
    {
        private readonly MongoDbService _mongoDbService;

        public ChatThreadService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        private IMongoCollection<TaskChatModel> GetChatCollection() =>
            _mongoDbService.GetDatabase().GetCollection<TaskChatModel>("taskchats");

        public async Task<IActionResult> AddThreadToTaskChatAsync(CreateThreadDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TaskId))
                return new BadRequestObjectResult(new { message = "TaskId is required." });

            var thread = new ThreadDetail
            {
                UserId = dto.UserId,
                UserName = dto.UserName,
                ConversationText = dto.ConversationText,
                DocumentId = dto.DocumentId
            };

            var filter = Builders<TaskChatModel>.Filter.Eq(c => c.TaskId, dto.TaskId);
            var update = Builders<TaskChatModel>.Update
                .SetOnInsert(c => c.OrganizationId, dto.OrganizationId)
                .SetOnInsert(c => c.EventId, dto.EventId)
                .SetOnInsert(c => c.CreatedOn, DateTime.UtcNow)
                .Set(c => c.UpdatedOn, DateTime.UtcNow)
                .Push(c => c.ThreadDetails, thread);

            var options = new UpdateOptions { IsUpsert = true };
            var result = await GetChatCollection().UpdateOneAsync(filter, update, options);

            return new OkObjectResult(new { message = "Thread added successfully." });
        }

        public async Task<IActionResult> GetTaskChatByTaskIdAsync(string taskId)
        {
            var chat = await GetChatCollection().Find(c => c.TaskId == taskId).FirstOrDefaultAsync();
            if (chat == null)
                return new NotFoundObjectResult(new { message = "No chat found for this Task." });

            return new OkObjectResult(chat);
        }
    }
}