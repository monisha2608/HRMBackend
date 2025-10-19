namespace HRMBackend.Dtos
{
    public class JobCreateRequest
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
    }

    public class JobUpdateRequest : JobCreateRequest { }

    public class JobResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime PostedOn { get; set; }
    }
}
