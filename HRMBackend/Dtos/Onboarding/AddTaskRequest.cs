using System.ComponentModel.DataAnnotations;

namespace HRMBackend.Dtos.Onboarding
{
    public class AddTaskRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? AssignedTo { get; set; }

        public DateTime? DueDate { get; set; }
    }
}
