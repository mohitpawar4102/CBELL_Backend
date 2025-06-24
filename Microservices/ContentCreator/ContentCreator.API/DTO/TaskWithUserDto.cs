using System;
using System.Collections.Generic;

namespace YourNamespace.DTO
{
    public class AssignedUserDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class TaskWithUserDto
    {
        public string Id { get; set; }
        public string EventId { get; set; }
        public string TaskTitle { get; set; }
        public string TaskStatus { get; set; }

        // Updated to use AssignedUserDto
        public List<AssignedUserDto> AssignedTo { get; set; } = new List<AssignedUserDto>();

        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public string CreativeType { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int CreativeNumbers { get; set; }
        public List<ChecklistItem> ChecklistDetails { get; set; }
        public string Description { get; set; }
        public string? OrganizationId { get; set; }
    }

    public class TaskWithUserAndDocumentsDto : TaskWithUserDto
    {
        public List<DocumentWithMetadataDto> Documents { get; set; } = new List<DocumentWithMetadataDto>();
    }
}
