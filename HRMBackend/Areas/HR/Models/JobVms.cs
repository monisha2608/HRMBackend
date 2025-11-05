using System.ComponentModel.DataAnnotations;

namespace HRMBackend.Areas.HR.Models
{
    public class JobCreateVm
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Description { get; set; } = default!;

        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
    }

    public class JobEditVm : JobCreateVm
    {
        public int Id { get; set; }
    }
}
