using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class DocumentDetails
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("OrganizationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? OrganizationId { get; set; }

    [BsonElement("EventId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? EventId { get; set; }

    [BsonElement("TaskId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? TaskId { get; set; }

    [BsonElement("ConversationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ConversationId { get; set; }

    [BsonElement("DocumentId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string DocumentId { get; set; } = null!; // Mandatory

    [BsonElement("InsertedOn")]
    public DateTime InsertedOn { get; set; } = DateTime.UtcNow;

    [BsonElement("UpdatedOn")]
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}
