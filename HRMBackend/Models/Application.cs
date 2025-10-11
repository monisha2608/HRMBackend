using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Backend.Models
{
    public class Application
    {
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public Job Job { get; set; } = default!;

        [Required]
        public string CandidateUserId { get; set; } = default!;

        [ForeignKey(nameof(CandidateUserId))]
        public AppUser CandidateUser { get; set; } = default!;

        [MaxLength(512)]
        public string? ResumeUrl { get; set; }

        public string? CoverLetter { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

        public DateTime AppliedOn { get; set; } = DateTime.UtcNow;
    }
}
