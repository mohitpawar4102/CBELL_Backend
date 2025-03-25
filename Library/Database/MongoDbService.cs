using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using YourNamespace.Models;

namespace YourNamespace.Library.Database
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;

        public MongoDbService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
            _database = client.GetDatabase(configuration.GetSection("MongoDbSettings")["DatabaseName"]);
        }

        //Its Does not fetch all the users, it provides access to MongoDb collection named "Users"
         public IMongoCollection<User> Users => _database.GetCollection<User>("Users");

         public IMongoCollection<Event> Events => _database.GetCollection<Event>("EventsMst");

    }
}
