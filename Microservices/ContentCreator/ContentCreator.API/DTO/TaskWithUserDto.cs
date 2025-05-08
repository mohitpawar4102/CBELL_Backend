public class TaskWithUserDto
{
    public string Id { get; set; }
    public string EventId { get; set; }
    public string TaskTitle { get; set; }
    public string TaskStatus { get; set; }

    // Now handles multiple assigned user names
    public List<string> AssignedTo { get; set; } = new List<string>();

    public int CreatedBy { get; set; }
    public int UpdatedBy { get; set; }
    public string CreativeType { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    public int CreativeNumbers { get; set; }
    public List<string> ChecklistDetails { get; set; }
    public string Description { get; set; }
    public string? OrganizationId { get; set; }
}
