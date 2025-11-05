namespace HRMBackend.Dtos
{
    public class ApplicationCreateRequest
    {
        public int JobId { get; set; }
        public string? ResumeUrl { get; set; }   // later: file upload -> URL
        public string? CoverLetter { get; set; }
    }

    public class ApplicationResponse
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime AppliedOn { get; set; }
        public string? ResumeUrl { get; set; }
        public string? CoverLetter { get; set; }
    }

    // HR-only update
    public class ApplicationStatusUpdateRequest
    {
        public string Status { get; set; } = default!; // Applied, UnderReview, Shortlisted, Rejected, InterviewScheduled, Offered, Hired
    }
}
