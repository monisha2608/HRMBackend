using HRMBackend.Data;
using HRM.Backend.Dtos.Applications;
using HRM.Backend.Models;
using HRM.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationsController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private readonly IShortlistScorer _scorer;
        private readonly IEmailSenderEx _mail;
        private readonly IVirusScanner _scanner;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ApplicationsController(
            ApplicationDbContext db,
            IConfiguration cfg,
            IShortlistScorer scorer,
            IEmailSenderEx mail,
            IVirusScanner scanner,
            IWebHostEnvironment env)
        {
            _db = db; _cfg = cfg; _scorer = scorer; _mail = mail; _scanner = scanner; _env = env;
        }

        // PUBLIC apply endpoint (no [Authorize]) so candidates can submit without an account.
        [HttpPost]
        [RequestSizeLimit(20_000_000)] // 20 MB
        public async Task<IActionResult> Apply([FromForm] ApplyRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == req.JobId);
            if (job == null) return BadRequest("Invalid JobId.");

            // --- Internal candidate rules ---
            // If the job is marked internal-only, force the checkbox + employee number.
           
            // If they ticked 'internal', still validate employee number.
            if (req.IsInternal && string.IsNullOrWhiteSpace(req.EmployeeNumber))
                return BadRequest("Please provide your employee number.");

            if (req.Resume is null || !IsAllowed(req.Resume))
                return BadRequest("Invalid resume file (only PDF/DOC/DOCX, <= 10 MB).");

            // AV scan
            using var ms = new MemoryStream();
            await req.Resume.CopyToAsync(ms);
            ms.Position = 0;
            var clean = await _scanner.IsCleanAsync(ms, req.Resume.FileName);
            if (!clean) return BadRequest("File failed security scan.");
            ms.Position = 0;

            // Save under wwwroot/uploads/resumes
            var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var resumesDir = Path.Combine(webRoot, "uploads", "resumes");
            Directory.CreateDirectory(resumesDir);
            var ext = Path.GetExtension(req.Resume.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var absPath = Path.Combine(resumesDir, fileName);
            await System.IO.File.WriteAllBytesAsync(absPath, ms.ToArray());
            var publicUrl = $"/uploads/resumes/{fileName}";

            // If candidate is logged in, link account; otherwise null.
            string? candidateUserId =
                (User?.Identity?.IsAuthenticated ?? false)
                    ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                    : null;

            var app = new Application
            {
                JobId = req.JobId,
                CandidateUserId = candidateUserId,
                AppliedOn = DateTime.UtcNow,
                Status = ApplicationStatus.Applied,
                ResumeUrl = publicUrl,
                CoverLetter = req.CoverLetter,
                ApplicantFullName = req.FullName,
                ApplicantEmail = req.Email,
                ApplicantPhone = req.Phone,

                IsInternal = req.IsInternal,
                EmployeeNumber = req.IsInternal ? req.EmployeeNumber?.Trim() : null
            };

            _db.Applications.Add(app);
            await _db.SaveChangesAsync();

            // Score + auto-shortlist
            var (score, reason) = _scorer.Score(job.Description ?? string.Empty, app.CoverLetter ?? string.Empty);

            // OPTIONAL: small bonus for internal candidates (tweak as you like)
            if (app.IsInternal) score = Math.Min(100, score + 5);
            app.Score = score;
            app.ShortlistReason = reason + (app.IsInternal? " (internal+5)" : "");

            var threshold = _cfg.GetValue<int>("Shortlist:DefaultThreshold", 60);
            if (score >= threshold && app.Status == ApplicationStatus.Applied)
            {
                var old = app.Status;
                app.Status = ApplicationStatus.Shortlisted;

                _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
                {
                    ApplicationId = app.Id,
                    OldStatus = old.ToString(),
                    NewStatus = app.Status.ToString(),
                    Note = $"Auto-shortlisted (score {score})",
                    ChangedByUserId = null
                });
            }

            await _db.SaveChangesAsync();

            // Email candidate (best-effort)
            if (!string.IsNullOrWhiteSpace(app.ApplicantEmail))
            {
                try
                {
                    await _mail.SendAsync(
                        app.ApplicantEmail!,
                        $"We received your application — {job.Title}",
                        MailTemplates.ApplicationReceived(app.ApplicantFullName ?? "Candidate", job.Title)
                    );
                }
                catch { /* ignore mail errors */ }
            }

            return CreatedAtAction(nameof(GetById), new { id = app.Id }, new { app.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var a = await _db.Applications
                .AsNoTracking()
                .Include(x => x.Job)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null) return NotFound();

            return Ok(new
            {
                a.Id,
                a.JobId,
                a.AppliedOn,
                status = a.Status.ToString(),
                a.ResumeUrl,
                a.ApplicantFullName,
                a.ApplicantEmail,
                a.ApplicantPhone,
                jobTitle = a.Job?.Title,

                // NEW —
                isInternal = a.IsInternal,
                employeeNumber = a.EmployeeNumber 
            });
        }

        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> Mine(
            [FromQuery] string? status,
            [FromQuery] string? sort = "new",
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            page = page < 1 ? 1 : page;
            size = size is < 1 or > 100 ? 10 : size;

            var q = _db.Applications
                .AsNoTracking()
                .Include(a => a.Job)
                .Where(a => a.CandidateUserId == userId);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ApplicationStatus>(status, out var st))
            {
                q = q.Where(a => a.Status == st);
            }

            q = (sort?.ToLowerInvariant() == "old")
                ? q.OrderBy(a => a.AppliedOn)
                : q.OrderByDescending(a => a.AppliedOn);

            var total = await q.CountAsync();

            var items = await q
                .Skip((page - 1) * size)
                .Take(size)
                .Select(a => new
                {
                    id = a.Id,
                    jobId = a.JobId,
                    jobTitle = a.Job!.Title,
                    appliedOn = a.AppliedOn,
                    status = a.Status.ToString(),
                    resumeUrl = a.ResumeUrl,

                    // NEW — handy on candidate dashboard
                    isInternal = a.IsInternal
                })
                .ToListAsync();

            return Ok(new { page, size, total, items });
        }

        // Re-usable file validation against config
        bool IsAllowed(IFormFile f)
        {
            var allowedExt = _cfg.GetSection("Uploads:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();
            var allowedMime = _cfg.GetSection("Uploads:AllowedMimeStarts").Get<string[]>() ?? Array.Empty<string>();
            var max = _cfg.GetValue<long>("Uploads:MaxSizeBytes", 10 * 1024 * 1024);

            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext)) return false;
            if (f.Length <= 0 || f.Length > max) return false;
            if (!string.IsNullOrEmpty(f.ContentType) &&
                !allowedMime.Any(m => f.ContentType.StartsWith(m, StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }
    }
}
