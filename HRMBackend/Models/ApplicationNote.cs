namespace HRM.Backend.Models
{
    public class ApplicationNote
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public Application Application { get; set; } = default!;
        public string AuthorUserId { get; set; } = default!;
        public string Body { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

}
