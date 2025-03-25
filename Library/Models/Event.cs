using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace YourNamespace.Models
{
    public class Event
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("EventName")]
        public string EventName { get; set; }

        [BsonElement("EventTypeId")]
        public int EventTypeId { get; set; }

        [BsonElement("EventTypeDesc")]
        public string EventTypeDesc { get; set; }

        [BsonElement("EventDescription")]
        public string EventDescription { get; set; }

        [BsonElement("LocationDetails")]
        public string LocationDetails { get; set; }

        [BsonElement("Dignitaries")]
        public string? Dignitaries { get; set; }

        [BsonElement("SpecialGuests")]
        public string? SpecialGuests { get; set; }

        [BsonElement("CreatedBy")]
        public int CreatedBy { get; set; }

        [BsonElement("CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [BsonElement("EventDate")]
        public DateTime EventDate { get; set; }

        [BsonElement("UpdatedBy")]
        public int UpdatedBy { get; set; }

        [BsonElement("UpdatedOn")]
        public DateTime UpdatedOn { get; set; }
    }
}
