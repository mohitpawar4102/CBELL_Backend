using System;

namespace YourNamespace.DTOs
{
    public class OrganizationDTO
    {
        // public string Id { get; set; }  // Now treated as a string, like in TaskDTO
        public string OrganizationName { get; set; }
        public int OrganizationStatus { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string OrganizationType { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOn { get; set; }
    }
}
