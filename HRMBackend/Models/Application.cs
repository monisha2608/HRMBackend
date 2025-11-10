using System.ComponentModel.DataAnnotations;

namespace HRM.Backend.Models
{
    public class Application
    {
        public int Id { get; set; }

        [Required]
        public int JobId { get; set; }
        public Job Job { get; set; } = default!;

        public string? CandidateUserId { get; set; }
        public AppUser? CandidateUser { get; set; }

        [Required]
        public DateTime AppliedOn { get; set; }

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

        public string? ResumeUrl { get; set; }

        [MaxLength(5000)]
        public string? CoverLetter { get; set; }

        // NEW: contact fields captured from the form (guest or explicit capture)
        [MaxLength(120)]
        public string? ApplicantFullName { get; set; }

        [MaxLength(200)]
        [EmailAddress]
        public string? ApplicantEmail { get; set; }

        [MaxLength(25)]
        public string? ApplicantPhone { get; set; }
        public int? Score { get; set; }           // for shortlisting
        public string? ShortlistReason { get; set; }

    }
}
