namespace YourNamespace.DTO
{
    public class EmailSendDto
    {
        public List<string> To { get; set; }
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string DocumentId { get; set; }
        public IFormFile? Attachment { get; set; }
    }
} 