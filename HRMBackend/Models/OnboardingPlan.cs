using System.ComponentModel.DataAnnotations;

namespace HRMBackend.Models
{
    public class OnboardingPlan
    {
        public int Id { get; set; }

        // Optional link to an Application (if you create the plan from a hired candidate)
        public int? ApplicationId { get; set; }

        [Required, MaxLength(160)]
        public string CandidateName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        // Navigation: list of tasks for this plan
        public ICollection<OnboardingTask> Tasks { get; set; } = new List<OnboardingTask>();
    }
}
