using MongoDB.Driver;
using YourNamespace.Models;
using YourNamespace.DTO;
using YourNamespace.Library.Database;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Library.Models;

namespace YourNamespace.Services
{
    public class DashboardService
    {
        private readonly MongoDbService _mongoDbService;

        public DashboardService(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        private IMongoCollection<Event> GetEventsCollection() =>
            _mongoDbService.GetDatabase().GetCollection<Event>("EventsMst");

        private IMongoCollection<TaskModel> GetTasksCollection() =>
            _mongoDbService.GetDatabase().GetCollection<TaskModel>("TasksMst");

        private IMongoCollection<OrganizationModel> GetOrganizationsCollection() =>
            _mongoDbService.GetDatabase().GetCollection<OrganizationModel>("OrganizationMst");

        public async Task<IActionResult> GetDashboardDataAsync(string organizationId)
        {
            Console.WriteLine($"[DashboardService] Received organizationId: '{organizationId}'");
            try
            {
                var activeEventsCount = await GetActiveEventsCountAsync(organizationId);
                var pendingTasksCount = await GetPendingTasksCountAsync(organizationId);
                var upcomingDeadlinesCount = await GetUpcomingDeadlineTasksCountAsync(organizationId);
                var eventsData = await GetEventsDataAsync(organizationId);
                var tasksData = await GetTasksDataAsync(organizationId);

                return new OkObjectResult(new
                {
                    ActiveEventsCount = activeEventsCount,
                    PendingTasksCount = pendingTasksCount,
                    UpcomingDeadlinesCount = upcomingDeadlinesCount,
                    Events = eventsData,
                    Tasks = tasksData
                });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = $"An error occurred: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        private async Task<int> GetActiveEventsCountAsync(string organizationId)
        {
            var filter = Builders<Event>.Filter.And(
                Builders<Event>.Filter.Gte(e => e.EventDate, DateTime.UtcNow),
                Builders<Event>.Filter.Eq(e => e.IsDeleted, false),
                Builders<Event>.Filter.Eq(e => e.OrganizationId, organizationId)
            );
            return (int)await GetEventsCollection().CountDocumentsAsync(filter);
        }

        private async Task<int> GetPendingTasksCountAsync(string organizationId)
        {
            var filter = Builders<TaskModel>.Filter.And(
                Builders<TaskModel>.Filter.In(t => t.TaskStatus, new[] { "New", "Active" }),
                Builders<TaskModel>.Filter.Eq(t => t.IsDeleted, false),
                Builders<TaskModel>.Filter.Eq(t => t.OrganizationId, organizationId)
            );
            return (int)await GetTasksCollection().CountDocumentsAsync(filter);
        }

        private async Task<int> GetUpcomingDeadlineTasksCountAsync(string organizationId)
        {
            var filter = Builders<TaskModel>.Filter.And(
                Builders<TaskModel>.Filter.In(t => t.TaskStatus, new[] { "New", "Active" }),
                Builders<TaskModel>.Filter.Lte(t => t.DueDate, DateTime.UtcNow.AddDays(3)),
                Builders<TaskModel>.Filter.Eq(t => t.IsDeleted, false),
                Builders<TaskModel>.Filter.Eq(t => t.OrganizationId, organizationId)
            );
            return (int)await GetTasksCollection().CountDocumentsAsync(filter);
        }

        private async Task<List<object>> GetEventsDataAsync(string organizationId)
        {
            Console.WriteLine($"[GetEventsDataAsync] Filtering EventsMst for OrganizationId: '{organizationId}'");
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument {
                    { "IsDeleted", false },
                    { "OrganizationId", organizationId }
                }),
                new BsonDocument("$addFields", new BsonDocument("OrganizationObjectId",
                    new BsonDocument("$toObjectId", "$OrganizationId"))),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "OrganizationMst" },
                    { "localField", "OrganizationObjectId" },
                    { "foreignField", "_id" },
                    { "as", "Organization" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$Organization" },
                    { "preserveNullAndEmptyArrays", true }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "EventName", 1 },
                    { "EventDate", 1 },
                    { "OrganizationName", "$Organization.OrganizationName" }
                })
            };

            var result = await GetEventsCollection()
                .Aggregate<BsonDocument>(pipeline)
                .ToListAsync();

            Console.WriteLine($"[GetEventsDataAsync] Events found: {result.Count}");

            return result.Select(doc => new
            {
                EventName = doc["EventName"].AsString,
                EventDate = doc["EventDate"].ToUniversalTime(),
                OrganizationName = doc.TryGetValue("OrganizationName", out var orgName) ? orgName.AsString : null
            }).Cast<object>().ToList();
        }

        private async Task<List<object>> GetTasksDataAsync(string organizationId)
        {
            Console.WriteLine($"[GetTasksDataAsync] Filtering TasksMst for OrganizationId: '{organizationId}'");
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument {
                    { "IsDeleted", false },
                    { "OrganizationId", organizationId }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "EventObjectId", new BsonDocument("$toObjectId", "$EventId") },
                    { "OrganizationObjectId", new BsonDocument("$toObjectId", "$OrganizationId") }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "EventsMst" },
                    { "localField", "EventObjectId" },
                    { "foreignField", "_id" },
                    { "as", "Event" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$Event" },
                    { "preserveNullAndEmptyArrays", true }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "OrganizationMst" },
                    { "localField", "OrganizationObjectId" },
                    { "foreignField", "_id" },
                    { "as", "Organization" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$Organization" },
                    { "preserveNullAndEmptyArrays", true }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "TaskTitle", 1 },
                    { "DueDate", 1 },
                    { "TaskStatus", 1 },
                    { "EventName", "$Event.EventName" },
                    { "OrganizationName", "$Organization.OrganizationName" }
                })
            };

            var result = await GetTasksCollection()
                .Aggregate<BsonDocument>(pipeline)
                .ToListAsync();

            Console.WriteLine($"[GetTasksDataAsync] Tasks found: {result.Count}");

            return result.Select(doc => new
            {
                TaskTitle = doc["TaskTitle"].AsString,
                DueDate = doc["DueDate"].ToUniversalTime(),
                TaskStatus = doc["TaskStatus"].AsString,
                EventName = doc.TryGetValue("EventName", out var evName) ? evName.AsString : null,
                OrganizationName = doc.TryGetValue("OrganizationName", out var orgName) ? orgName.AsString : null
            }).Cast<object>().ToList();
        }
    }
}
