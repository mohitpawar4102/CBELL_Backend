using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace YourNamespace.Models
{
    public class Event
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString(); // MongoDB uses string for ObjectId
        public string EventName { get; set; }
        public string EventTypeId { get; set; }
        public string EventTypeDesc { get; set; }
        public string EventDescription { get; set; }
        public string LocationDetails { get; set; }
        public string Dignitaries { get; set; }
        public string SpecialGuests { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime EventDate { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string OrganizationId { get; set; }
    }
}
