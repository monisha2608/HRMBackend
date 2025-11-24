using System.Text;
using HRMBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMBackend.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(AuthenticationSchemes = "HR", Roles = "HR")]
    [Route("api/hr/[controller]")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ReportsController(ApplicationDbContext db) { _db = db; }

        // GET /api/hr/reports/applications-csv?jobId=123
        [HttpGet("applications-csv")]
        public async Task<IActionResult> ApplicationsCsv([FromQuery] int jobId)
        {
            var job = await _db.Jobs.FindAsync(jobId);
            if (job == null) return NotFound("Job not found.");

            var rows = await _db.Applications
              .Include(a => a.CandidateUser)
              .Where(a => a.JobId == jobId)
              .OrderByDescending(a => a.AppliedOn)
              .Select(a => new {
                  a.Id,
                  a.AppliedOn,
                  Status = a.Status.ToString(),
                  ResumeUrl = a.ResumeUrl ?? "",
                  CandidateName = a.CandidateUser != null ? a.CandidateUser.FullName : "",
                  CandidateEmail = a.CandidateUser != null ? a.CandidateUser.Email : "",
                  a.ApplicantFullName,
                  a.ApplicantEmail,
                  a.ApplicantPhone
              })
              .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,AppliedOnUTC,Status,CandidateName,CandidateEmail,ApplicantFullName,ApplicantEmail,ApplicantPhone,ResumeUrl");
            foreach (var r in rows)
            {
                // basic CSV escaping
                string Esc(string? s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";
                sb.AppendLine(string.Join(",",
                  r.Id,
                  Esc(r.AppliedOn.ToString("u")),
                  Esc(r.Status),
                  Esc(r.CandidateName),
                  Esc(r.CandidateEmail),
                  Esc(r.ApplicantFullName),
                  Esc(r.ApplicantEmail),
                  Esc(r.ApplicantPhone),
                  Esc(r.ResumeUrl)
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"applications_job_{jobId}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
    }
}
