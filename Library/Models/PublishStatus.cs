public class PublishStatus
{
    public string Platform { get; set; }             // e.g., Facebook, Instagram
    public bool IsPublished { get; set; }            // true if publishing succeeded
    public string PublishedById { get; set; }        // UserId of the person who published
    public string PublishedByName { get; set; }      // UserName of the person who published
    public DateTime? PublishedAt { get; set; }       // Timestamp of publishing
}
