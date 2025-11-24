using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMBackend.Models
{
    public class OnboardingTask
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Plan))]
        public int PlanId { get; set; }
        public OnboardingPlan Plan { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? AssignedTo { get; set; }

        public DateTime? DueDate { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime? CompletedOn { get; set; }
    }
}
