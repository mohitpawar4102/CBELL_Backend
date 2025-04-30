using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class TaskChatModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string OrganizationId { get; set; }
    public string EventId { get; set; }
    public string TaskId { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    public List<ThreadDetail> ThreadDetails { get; set; } = new();
}

public class ThreadDetail
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string ThreadId { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    public string UserId { get; set; }
    public string ConversationText { get; set; }
    
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> DocumentId { get; set; } = new();
}
