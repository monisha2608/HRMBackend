using HRM.Backend.Models;

namespace HRMBackend.Areas.HR.Models
{
    public class ApplicationDetailsVM
    {
        public Application Application { get; set; } = default!;
        public int? Score { get; set; }
        public string? ShortlistReason { get; set; }

        public IEnumerable<ApplicationStatusHistory> History { get; set; } = Enumerable.Empty<ApplicationStatusHistory>();
        public IEnumerable<ApplicationNote> Notes { get; set; } = Enumerable.Empty<ApplicationNote>();
    }
}
