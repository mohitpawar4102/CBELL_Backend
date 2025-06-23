using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Library.Models
{
    public class Document
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("fileId")]
        public ObjectId FileId { get; set; }

        [BsonElement("fileName")]
        public string FileName { get; set; }

        [BsonElement("contentType")]
        public string ContentType { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("uploadedAt")]
        public DateTime UploadedAt { get; set; }

        [BsonElement("isDeleted")]
        public bool IsDeleted { get; set; }

        [BsonElement("deletedOn")]
        public DateTime? DeletedOn { get; set; }

        public string Status { get; set; } = "Pending"; // "Pending", "Approved"
        public List<PublishStatus> PublishedTo { get; set; } = new();

    }
}
