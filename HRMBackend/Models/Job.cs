using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Backend.Models
{
    public class Job
    {
        public int Id { get; set; }

        [Required, MaxLength(160)]
        public string Title { get; set; } = default!;

        [Required]
        public string Description { get; set; } = default!;

        [MaxLength(80)]
        public string? Department { get; set; }

        [MaxLength(80)]
        public string? Location { get; set; }

        [MaxLength(40)]
        public string? EmploymentType { get; set; } // e.g. Full-time

        public DateTime PostedOn { get; set; } = DateTime.UtcNow;

        [Required]
        public string PostedByUserId { get; set; } = default!;

        [ForeignKey(nameof(PostedByUserId))]
        public AppUser PostedByUser { get; set; } = default!;
    }
}
