using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Library.Models
{
    public class OrganizationModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("OrganizationName")]
        public string OrganizationName { get; set; }

        [BsonElement("OrganizationStatus")]
        public int OrganizationStatus { get; set; }

        [BsonElement("OrganizationType")]
        public string OrganizationType { get; set; }

        [BsonElement("CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [BsonElement("UpdatedOn")]
        public DateTime UpdatedOn { get; set; }

        [BsonElement("IsDeleted")]
        public bool IsDeleted { get; set; }

        [BsonElement("DeletedOn")]
        public DateTime? DeletedOn { get; set; }

        [BsonElement("OrganizationCode")]
        public string OrganizationCode { get; set; }
    }
}
