// File: Library/Helpers/MongoDbHelper.cs
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace YourNamespace.Library.Helpers
{
    public static class MongoDbHelper
    {
        public static async Task<bool> IdExistsAsync(IMongoDatabase db, string collectionName, string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
                return false;

            var collection = db.GetCollection<BsonDocument>(collectionName);
            return await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", objectId)).AnyAsync();
        }
    }
}
