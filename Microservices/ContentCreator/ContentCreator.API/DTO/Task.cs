using System.Text.Json.Serialization;

namespace YourNamespace.DTO
{
    public class TaskDTO
    {
        public string EventId { get; set; }
        public string TaskTitle { get; set; }
        public int TaskStatus { get; set; }
        public int AssignedTo { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public string CreativeType { get; set; }
        public DateTime DueDate { get; set; }
        public int CreativeNumbers { get; set; }
        public string? OrganizationId { get; set; }

        [JsonPropertyName("ChecklistDetails")]
        public List<string> ChecklistDetails { get; set; } = new List<string>();
        public string Description { get; set; }
        // Soft Delete Fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedOn { get; set; }
    }
}