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

        public async Task<IActionResult> GetDashboardDataAsync()
        {
            try
            {
                var activeEventsCount = await GetActiveEventsCountAsync();
                var pendingTasksCount = await GetPendingTasksCountAsync();
                var upcomingDeadlinesCount = await GetUpcomingDeadlineTasksCountAsync();
                var eventsData = await GetEventsDataAsync();
                var tasksData = await GetTasksDataAsync();

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

        private async Task<int> GetActiveEventsCountAsync()
        {
            var filter = Builders<Event>.Filter.And(
                Builders<Event>.Filter.Gte(e => e.EventDate, DateTime.UtcNow),
                Builders<Event>.Filter.Eq(e => e.IsDeleted, false)
            );
            return (int)await GetEventsCollection().CountDocumentsAsync(filter);
        }

        private async Task<int> GetPendingTasksCountAsync()
        {
            var filter = Builders<TaskModel>.Filter.And(
                Builders<TaskModel>.Filter.In(t => t.TaskStatus, new[] { "New", "Active" }),
                Builders<TaskModel>.Filter.Eq(t => t.IsDeleted, false)
            );
            return (int)await GetTasksCollection().CountDocumentsAsync(filter);
        }

        private async Task<int> GetUpcomingDeadlineTasksCountAsync()
        {
            var filter = Builders<TaskModel>.Filter.And(
                Builders<TaskModel>.Filter.In(t => t.TaskStatus, new[] { "New", "Active" }),
                Builders<TaskModel>.Filter.Lte(t => t.DueDate, DateTime.UtcNow.AddDays(3)),
                Builders<TaskModel>.Filter.Eq(t => t.IsDeleted, false)
            );
            return (int)await GetTasksCollection().CountDocumentsAsync(filter);
        }

        private async Task<List<object>> GetEventsDataAsync()
        {
            var pipeline = new[]
            {
        new BsonDocument("$match", new BsonDocument("IsDeleted", false)),

        // Convert string OrganizationId to ObjectId
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

            return result.Select(doc => new
            {
                EventName = doc["EventName"].AsString,
                EventDate = doc["EventDate"].ToUniversalTime(),
                OrganizationName = doc.TryGetValue("OrganizationName", out var orgName) ? orgName.AsString : null
            }).Cast<object>().ToList();
        }

        private async Task<List<object>> GetTasksDataAsync()
        {
            var pipeline = new[]
            {
        new BsonDocument("$match", new BsonDocument("IsDeleted", false)),

        // Convert EventId and OrganizationId to ObjectIds
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
