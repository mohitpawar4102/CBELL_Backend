using MongoDB.Driver;
using YourNamespace.Models;
using YourNamespace.DTO;
using YourNamespace.Library.Database;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

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
                var organizationDict = await GetOrganizationDictionaryAsync();
                var eventsData = await GetEventsDataAsync(organizationDict);
                var tasksData = await GetTasksDataAsync(organizationDict);

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

        private async Task<Dictionary<string, string>> GetOrganizationDictionaryAsync()
        {
            var orgs = await GetOrganizationsCollection().Find(o => !o.IsDeleted).ToListAsync();
            return orgs.ToDictionary(o => o.Id, o => o.OrganizationName);
        }

        private async Task<List<object>> GetEventsDataAsync(Dictionary<string, string> organizationDict)
        {
            var events = await GetEventsCollection()
                .Find(e => !e.IsDeleted)
                .ToListAsync();

            var result = events.Select(e => new
            {
                e.EventName,
                e.EventDate,
                OrganizationName = organizationDict.ContainsKey(e.OrganizationId) ? organizationDict[e.OrganizationId] : "Unknown Organization"
            });

            return result.Cast<object>().ToList();
        }

        private async Task<List<object>> GetTasksDataAsync(Dictionary<string, string> organizationDict)
        {
            var tasks = await GetTasksCollection()
                .Find(t => !t.IsDeleted)
                .ToListAsync();

            var eventIds = tasks.Select(t => t.EventId).Distinct().ToList();
            var events = await GetEventsCollection()
                .Find(e => eventIds.Contains(e.Id))
                .ToListAsync();

            var eventDict = events.ToDictionary(e => e.Id, e => e.EventName);

            var taskDtos = tasks.Select(t => new
            {
                t.TaskTitle,
                t.DueDate,
                EventName = eventDict.ContainsKey(t.EventId) ? eventDict[t.EventId] : "Unknown Event",
                OrganizationName = organizationDict.ContainsKey(t.OrganizationId) ? organizationDict[t.OrganizationId] : "Unknown Organization"
            });

            return taskDtos.Cast<object>().ToList();
        }
    }
}
