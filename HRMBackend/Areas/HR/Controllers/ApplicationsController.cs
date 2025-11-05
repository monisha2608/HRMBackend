using HRM.Backend.Data;
using HRM.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMBackend.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(AuthenticationSchemes = "HR", Roles = "HR")]
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ApplicationsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index(int jobId, string? status, string? q)
        {
            var job = await _db.Jobs.FindAsync(jobId);
            if (job == null) return NotFound();

            var apps = _db.Applications
                .Include(a => a.CandidateUser)
                .Where(a => a.JobId == jobId);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ApplicationStatus>(status, true, out var st))
                apps = apps.Where(a => a.Status == st);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                apps = apps.Where(a =>
                    EF.Functions.Like(a.CandidateUser!.Email!, $"%{q}%") ||
                    EF.Functions.Like(a.CandidateUser!.FullName!, $"%{q}%") ||
                    EF.Functions.Like(a.ApplicantEmail!, $"%{q}%") ||
                    EF.Functions.Like(a.ApplicantFullName!, $"%{q}%") ||
                    EF.Functions.Like(a.ApplicantPhone!, $"%{q}%"));
            }

            var list = await apps
                .OrderByDescending(a => a.AppliedOn)
                .Select(a => new AppVM
                {
                    Id = a.Id,
                    CandidateEmail = a.CandidateUser != null ? a.CandidateUser.Email : null,
                    CandidateName = a.CandidateUser != null ? a.CandidateUser.FullName : null,
                    ApplicantFullName = a.ApplicantFullName,
                    ApplicantEmail = a.ApplicantEmail,
                    ApplicantPhone = a.ApplicantPhone,
                    Status = a.Status.ToString(),
                    AppliedOn = a.AppliedOn,
                    ResumeUrl = a.ResumeUrl
                })
                .ToListAsync();

            ViewBag.Job = job;
            ViewBag.Status = status;
            ViewBag.Query = q;
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus, int jobId)
        {
            var app = await _db.Applications.FindAsync(id);
            if (app == null) return NotFound();

            if (!Enum.TryParse<ApplicationStatus>(newStatus, true, out var st))
            {
                TempData["Err"] = "Invalid status.";
                return RedirectToAction(nameof(Index), new { jobId });
            }
            app.Status = st;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { jobId });
        }

        public class AppVM
        {
            public int Id { get; set; }
            public string? CandidateEmail { get; set; }
            public string? CandidateName { get; set; }
            public string? ApplicantFullName { get; set; }
            public string? ApplicantEmail { get; set; }
            public string? ApplicantPhone { get; set; }
            public string Status { get; set; } = default!;
            public DateTime AppliedOn { get; set; }
            public string? ResumeUrl { get; set; }
        }
    }
}
