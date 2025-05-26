using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace YourNamespace.DTO
{
    public class ChecklistItem
    {
        public string Text { get; set; }
        public bool Checked { get; set; }
        public bool IsPlaceholder { get; set; }
    }

    public class TaskDTO
    {
        public string EventId { get; set; }
        public string TaskTitle { get; set; }
        public string TaskStatus { get; set; }

        // Change: Now supports a list of user IDs for AssignedTo
        public List<string> AssignedTo { get; set; } = new List<string>();
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public string CreativeType { get; set; }
        public DateTime DueDate { get; set; }
        public int CreativeNumbers { get; set; }
        public string? OrganizationId { get; set; }

        [JsonPropertyName("ChecklistDetails")]
        public List<ChecklistItem> ChecklistDetails { get; set; } = new List<ChecklistItem>();

        public string Description { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedOn { get; set; }
    }
}
