namespace HRMBackend.Dtos
{
    public class HrSummaryResponse
    {
        public int TotalJobs { get; set; }
        public int TotalApplications { get; set; }
        public Dictionary<string, int> ApplicationsByStatus { get; set; } = new();
        public IEnumerable<DailyCount> ApplicationsLast7Days { get; set; } = Array.Empty<DailyCount>();
    }

    public class DailyCount
    {
        public DateOnly Day { get; set; }
        public int Count { get; set; }
    }

    public class JobListItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime PostedOn { get; set; }
        public int ApplicantsCount { get; set; }
    }

    public class JobDetailResponse : JobListItemResponse
    {
        public string Description { get; set; } = default!;
    }

    public class ApplicationListItemResponse
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = default!;
        public string CandidateEmail { get; set; } = default!;
        public string? CandidateName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime AppliedOn { get; set; }
        public string? ResumeUrl { get; set; }
    }

    public class CandidateListItemResponse
    {
        public string UserId { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? FullName { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int ApplicationsCount { get; set; }
    }
}
