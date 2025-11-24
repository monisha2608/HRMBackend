namespace HRMBackend.Dtos.Onboarding
{
    public class UpdateTaskRequest
    {
        public string? Name { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? DueDate { get; set; }
        public bool? IsCompleted { get; set; }
    }
}
