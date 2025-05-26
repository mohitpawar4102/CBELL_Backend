namespace YourNamespace.DTO
{
    public class DocumentWithMetadataDto
    {
        public string DocumentId { get; set; }
        public string Filename { get; set; }
        public long Length { get; set; }
        public int ChunkSize { get; set; }
        public DateTime? UploadDate { get; set; }
        public string ContentType { get; set; }
        public string Description { get; set; }
    }
}
