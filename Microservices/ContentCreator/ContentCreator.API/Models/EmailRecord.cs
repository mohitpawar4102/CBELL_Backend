using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace YourNamespace.Models
{
    public class EmailRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public List<string> To { get; set; }
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string DocumentId { get; set; }
        public string? AttachmentFileName { get; set; }
        public DateTime SentAt { get; set; }
        public string Status { get; set; }
    }
} 