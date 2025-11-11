using HRM.Backend.Data;
using HRM.Backend.Models;
using HRMBackend.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HRMBackend.Controllers
{
    [ApiController]
    [Route("api/hr")]
    [Authorize(Roles = "HR")]
    public class HrController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public HrController(ApplicationDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ====== SUMMARY ======
        // GET /api/hr/summary
        [HttpGet("summary")]
        public async Task<ActionResult<HrSummaryResponse>> Summary()
        {
            var totalJobs = await _db.Jobs.CountAsync();
            var totalApps = await _db.Applications.CountAsync();

            var byStatus = await _db.Applications
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);
            var last7 = await _db.Applications
                .Where(a => a.AppliedOn >= sevenDaysAgo)
                .GroupBy(a => DateOnly.FromDateTime(a.AppliedOn))
                .Select(g => new DailyCount { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            // Ensure all 7 days exist in response
            var days = Enumerable.Range(0, 7).Select(i => DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-6 + i))).ToList();
            var merged = days.Select(d => last7.FirstOrDefault(x => x.Day == d) ?? new DailyCount { Day = d, Count = 0 });

            return Ok(new HrSummaryResponse
            {
                TotalJobs = totalJobs,
                TotalApplications = totalApps,
                ApplicationsByStatus = byStatus,
                ApplicationsLast7Days = merged
            });
        }

        // ====== JOBS LIST (paged + search) ======
        // POST /api/hr/jobs/search
        [HttpPost("jobs/search")]
        public async Task<ActionResult<PagedResult<JobListItemResponse>>> JobsSearch([FromBody] PagedRequest req)
        {
            var page = Math.Max(1, req.Page);
            var size = Math.Clamp(req.PageSize, 1, 100);
            var q = _db.Jobs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.Trim();
                q = q.Where(j =>
                    EF.Functions.Like(j.Title, $"%{s}%") ||
                    EF.Functions.Like(j.Department!, $"%{s}%") ||
                    EF.Functions.Like(j.Location!, $"%{s}%"));
            }

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(j => j.PostedOn)
                .Select(j => new JobListItemResponse
                {
                    Id = j.Id,
                    Title = j.Title,
                    Department = j.Department,
                    Location = j.Location,
                    EmploymentType = j.EmploymentType,
                    PostedOn = j.PostedOn,
                    ApplicantsCount = _db.Applications.Count(a => a.JobId == j.Id)
                })
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return Ok(new PagedResult<JobListItemResponse>
            {
                Page = page,
                PageSize = size,
                Total = total,
                Items = items
            });
        }

        // ====== JOB DETAILS ======
        // GET /api/hr/jobs/{id}
        [HttpGet("jobs/{id:int}")]
        public async Task<ActionResult<JobDetailResponse>> JobDetail(int id)
        {
            var j = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (j == null) return NotFound();

            var count = await _db.Applications.CountAsync(a => a.JobId == id);
            return Ok(new JobDetailResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Department = j.Department,
                Location = j.Location,
                EmploymentType = j.EmploymentType,
                PostedOn = j.PostedOn,
                ApplicantsCount = count
            });
        }

        // ====== APPLICANTS PER JOB (paged + status filter) ======
        // POST /api/hr/jobs/{jobId}/applications
        [HttpPost("jobs/{jobId:int}/applications")]
        public async Task<ActionResult<PagedResult<ApplicationListItemResponse>>> ApplicationsByJob(int jobId, [FromBody] PagedRequest req)
        {
            var exists = await _db.Jobs.AnyAsync(j => j.Id == jobId);
            if (!exists) return NotFound(new { error = "Job not found." });

            var page = Math.Max(1, req.Page);
            var size = Math.Clamp(req.PageSize, 1, 100);

            var q = _db.Applications
                .AsNoTracking()
                .Include(a => a.CandidateUser)
                .Include(a => a.Job)
                .Where(a => a.JobId == jobId);

            if (!string.IsNullOrWhiteSpace(req.Status) &&
                Enum.TryParse<ApplicationStatus>(req.Status, true, out var st))
            {
                q = q.Where(a => a.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.Trim();
                q = q.Where(a =>
                    EF.Functions.Like(a.CandidateUser.Email!, $"%{s}%") ||
                    EF.Functions.Like(a.CandidateUser.FullName!, $"%{s}%") ||
                    EF.Functions.Like(a.Job.Title, $"%{s}%"));
            }

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(a => a.AppliedOn)
                .Select(a => new ApplicationListItemResponse
                {
                    Id = a.Id,
                    JobId = a.JobId,
                    JobTitle = a.Job.Title,
                    CandidateEmail = a.CandidateUser.Email!,
                    CandidateName = a.CandidateUser.FullName,
                    Status = a.Status.ToString(),
                    AppliedOn = a.AppliedOn,
                    ResumeUrl = a.ResumeUrl
                })
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return Ok(new PagedResult<ApplicationListItemResponse>
            {
                Page = page,
                PageSize = size,
                Total = total,
                Items = items
            });
        }

        // ====== CANDIDATES DIRECTORY (paged + search) ======
        // POST /api/hr/candidates/search
        [HttpPost("candidates/search")]
        public async Task<ActionResult<PagedResult<CandidateListItemResponse>>> CandidatesSearch([FromBody] PagedRequest req)
        {
            var page = Math.Max(1, req.Page);
            var size = Math.Clamp(req.PageSize, 1, 100);

            // Get candidate role users
            var candidateRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Candidate");
            if (candidateRole == null) return Ok(new PagedResult<CandidateListItemResponse> { Page = page, PageSize = size, Total = 0 });

            var userIds = await _db.UserRoles
                .Where(ur => ur.RoleId == candidateRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var q = _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.Trim();
                q = q.Where(u =>
                    EF.Functions.Like(u.Email!, $"%{s}%") ||
                    EF.Functions.Like(u.FullName!, $"%{s}%"));
            }

            var total = await q.CountAsync();

            var items = await q
                .OrderBy(u => u.Email)
                .Select(u => new CandidateListItemResponse
                {
                    UserId = u.Id,
                    Email = u.Email!,
                    FullName = u.FullName,
                    CreatedUtc = u.LockoutEnd.HasValue
        ? u.LockoutEnd.Value.UtcDateTime
        : DateTime.UtcNow,
                    ApplicationsCount = _db.Applications.Count(a => a.CandidateUserId == u.Id)
                })

                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return Ok(new PagedResult<CandidateListItemResponse>
            {
                Page = page,
                PageSize = size,
                Total = total,
                Items = items
            });
        }

        // ====== CSV EXPORT (applications) ======
        // GET /api/hr/applications/export?jobId=123   (if jobId omitted -> all)
        [HttpGet("applications/export")]
        public async Task<IActionResult> ExportCsv([FromQuery] int? jobId)
        {
            var q = _db.Applications
                .AsNoTracking()
                .Include(a => a.Job)
                .Include(a => a.CandidateUser)
                .AsQueryable();

            if (jobId.HasValue)
            {
                var exists = await _db.Jobs.AnyAsync(j => j.Id == jobId.Value);
                if (!exists) return NotFound(new { error = "Job not found." });
                q = q.Where(a => a.JobId == jobId.Value);
            }

            var rows = await q
                .OrderByDescending(a => a.AppliedOn)
                .Select(a => new
                {
                    a.Id,
                    a.JobId,
                    JobTitle = a.Job.Title,
                    a.CandidateUser.Email,
                    a.CandidateUser.FullName,
                    Status = a.Status.ToString(),
                    AppliedOn = a.AppliedOn,
                    a.ResumeUrl
                })
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Id,JobId,JobTitle,CandidateEmail,CandidateName,Status,AppliedOn,ResumeUrl");

            foreach (var r in rows)
            {
                // naive CSV escaping; for production use a CSV library
                string esc(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
                csv.AppendLine(string.Join(",", new[]
                {
                    r.Id.ToString(CultureInfo.InvariantCulture),
                    r.JobId.ToString(CultureInfo.InvariantCulture),
                    esc(r.JobTitle),
                    esc(r.Email ?? ""),
                    esc(r.FullName ?? ""),
                    esc(r.Status),
                    esc(r.AppliedOn.ToString("u")),
                    esc(r.ResumeUrl ?? "")
                }));
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", jobId.HasValue ? $"applications_job_{jobId.Value}.csv" : "applications_all.csv");
        }
    }
}
