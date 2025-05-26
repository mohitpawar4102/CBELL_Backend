using System;
using System.Collections.Generic;

namespace YourNamespace.DTOs
{
    public class TaskChatDto
    {
        public string TaskId { get; set; }
        public string EventId { get; set; }
        public string OrganizationId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public List<ThreadDetailDto> ThreadDetails { get; set; }
    }
}
