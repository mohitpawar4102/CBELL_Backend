namespace YourNamespace.DTOs
{
    public class EventDto
    {
        public string? Id { get; set; }
        public string EventName { get; set; }
        public string EventTypeId { get; set; }
        public string EventTypeDesc { get; set; }
        public string EventDescription { get; set; }
        public string LocationDetails { get; set; }
        public List<string> Coordinators { get; set; } = new List<string>();
        public List<string> SpecialGuests { get; set; } = new List<string>();
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime EventDate { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string OrganizationId {get; set;}
        // Soft Delete Fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedOn { get; set; }
    }
}
