using Library.Models;

namespace YourNamespace.DTO
{
    public class DocumentWithMetadataDto
    {
        public string DocumentId { get; set; }
        public string Filename { get; set; }
        public string ContentType { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Id { get; set; } 
        public List<PublishStatus> PublishedTo { get; set; }
        public string FileId { get; set; }
    }
}
