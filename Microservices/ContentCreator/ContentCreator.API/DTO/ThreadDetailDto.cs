using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace YourNamespace.DTOs
{
    public class ThreadDetailDto
    {
        public ObjectId ConversationId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ConversationText { get; set; }
        public List<string> DocumentId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
