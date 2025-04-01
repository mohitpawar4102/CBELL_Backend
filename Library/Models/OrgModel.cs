using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace YourNamespace.Models
{
    public class OrganizationModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]  // Handles string ↔ ObjectId conversion automatically
        public string Id { get; set; }  

        public string OrganizationName { get; set; }
        public int OrganizationStatus { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string OrganizationType { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOn { get; set; }
    }
}
