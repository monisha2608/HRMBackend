using System.ComponentModel.DataAnnotations;

namespace HRMBackend.Dtos.Onboarding
{
    public class CreatePlanRequest
    {
        public int? ApplicationId { get; set; }

        [Required, MaxLength(160)]
        public string CandidateName { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
    }
}
