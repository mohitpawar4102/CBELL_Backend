using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace YourNamespace.Models
{
    public class TaskModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public required string EventId { get; set; }
        public required string TaskTitle { get; set; }
        public string TaskStatus { get; set; }
        public int AssignedTo { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public string? CreativeType { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int CreativeNumbers { get; set; }

        [BsonElement("ChecklistDetails")]
        public List<string> ChecklistDetails { get; set; } = new List<string>();
        public string? Description { get; set; }
        public string OrganizationId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedOn { get; set; }
    }
}
