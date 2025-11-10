using HRM.Backend.Data;
using HRM.Backend.Models;
using HRMBackend.Areas.HR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using HRM.Backend.Services; // <-- for IEmailSenderEx

namespace HRMBackend.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(AuthenticationSchemes = "HR", Roles = "HR")]
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSenderEx _mail;
        private readonly ILogger<ApplicationsController> _log;

        public ApplicationsController(
            ApplicationDbContext db,
            IEmailSenderEx mail,
            ILogger<ApplicationsController> log)
        {
            _db = db;
            _mail = mail;
            _log = log;
        }
        // LIST
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
                    ResumeUrl = a.ResumeUrl,
                    Score = a.Score,                 // NEW
                    ShortlistReason = a.ShortlistReason        // NEW
                })
                .ToListAsync();

            ViewBag.Job = job;
            ViewBag.Status = status;
            ViewBag.Query = q;
            return View(list);
        }

        // DETAILS (with timeline + notes)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var app = await _db.Applications
                .Include(a => a.Job)
                .Include(a => a.CandidateUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null) return NotFound();

            var vm = new ApplicationDetailsVM
            {
                Application = app,
                History = await _db.ApplicationStatusHistories
                    .Where(h => h.ApplicationId == id)
                    .OrderByDescending(h => h.ChangedAtUtc)
                    .ToListAsync(),
                Notes = await _db.ApplicationNotes
                    .Where(n => n.ApplicationId == id)
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .ToListAsync()
            };

            return View(vm);
        }

        // STATUS UPDATE (records history + emails candidate)
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

            var old = app.Status;
            if (old != st)
            {
                app.Status = st;

                _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
                {
                    ApplicationId = app.Id,
                    OldStatus = old.ToString(),
                    NewStatus = st.ToString(),
                    ChangedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    Note = "Updated by HR"
                });

                await _db.SaveChangesAsync();
                TempData["Msg"] = "Status updated.";

                // Send candidate email (best-effort, non-blocking for UX)
                try
                {
                    // Load job title for email subject/body
                    var job = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == app.JobId);
                    var jobTitle = job?.Title ?? "your application";

                    if (!string.IsNullOrWhiteSpace(app.ApplicantEmail))
                    {
                        var friendly = app.ApplicantFullName ?? "Candidate";
                        var statusText = app.Status.ToString();
                        var body = MailTemplates.StatusChanged(friendly, jobTitle, statusText);

                        await _mail.SendAsync(
                            app.ApplicantEmail!,
                            $"Your application status was updated — {jobTitle}",
                            body
                        );
                    }
                }
                catch
                {
                    // swallow email errors; do not break HR flow
                }
            }

            return RedirectToAction(nameof(Index), new { jobId });
        }

        // ADD NOTE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int id, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                TempData["Err"] = "Note cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var app = await _db.Applications.FindAsync(id);
            if (app == null) return NotFound();

            _db.ApplicationNotes.Add(new ApplicationNote
            {
                ApplicationId = id,
                AuthorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                Body = body.Trim()
            });

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Note added.";
            return RedirectToAction(nameof(Details), new { id });
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

            // NEW — to display auto-shortlist info on list page
            public int? Score { get; set; }
            public string? ShortlistReason { get; set; }
        }
    }
}
