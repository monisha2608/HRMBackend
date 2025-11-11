using System.ComponentModel.DataAnnotations;

namespace HRM.Backend.Dtos.Applications
{
    public class ApplyRequest
    {
        [Required]
        public int JobId { get; set; }

        [Required, StringLength(120)]
        public string FullName { get; set; } = default!;

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = default!;

        [Required, StringLength(25)]
        public string Phone { get; set; } = default!;

        [Required]
        public IFormFile Resume { get; set; } = default!;

        [Required, StringLength(5000)]
        public string CoverLetter { get; set; } = default!;
    }
}
