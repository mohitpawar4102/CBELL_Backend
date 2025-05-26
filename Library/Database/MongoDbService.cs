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
         public IMongoDatabase GetDatabase() => _database;
    }
}
