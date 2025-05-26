using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace YourNamespace.Models
{
    public class EventTypeModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string TypeName { get; set; } = string.Empty;

        public string TypeDescription { get; set; } = string.Empty;
    }
}
