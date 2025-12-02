using System.ComponentModel.DataAnnotations;

namespace HRMBackend.Models
{
    public class SuccessionRecord
    {
        public int Id { get; set; }

        // Link to the hired Application
        [Required]
        public int ApplicationId { get; set; }

        [Required, MaxLength(160)]
        public string CandidateName { get; set; } = string.Empty;

        [MaxLength(160)]
        public string CurrentRole { get; set; } = string.Empty;

        [MaxLength(160)]
        public string? PotentialNextRole { get; set; }

        // e.g. "ReadyNow", "Within1To2Years", "Beyond2Years"
        [MaxLength(40)]
        public string Readiness { get; set; } = "Within1To2Years";

        // e.g. "Low", "Medium", "High"
        [MaxLength(20)]
        public string RiskOfLoss { get; set; } = "Medium";

        [MaxLength(1000)]
        public string? DevelopmentNotes { get; set; }

        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
