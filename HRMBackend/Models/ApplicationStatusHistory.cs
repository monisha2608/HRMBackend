namespace HRM.Backend.Models
{
    public class ApplicationStatusHistory
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public Application Application { get; set; } = default!;
        public string OldStatus { get; set; } = default!;
        public string NewStatus { get; set; } = default!;
        public string? Note { get; set; }
        public string? ChangedByUserId { get; set; }
        public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    }

}
