namespace YourNamespace.DTO
{
    public class CreateThreadDto
    {
        public string OrganizationId { get; set; }
        public string EventId { get; set; }
        public string TaskId { get; set; }

        public string UserId { get; set; }
        public string ConversationText { get; set; }
        public List<string> DocumentId { get; set; }
    }
}
