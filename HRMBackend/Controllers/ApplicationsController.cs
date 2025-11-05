using HRM.Backend.Data;
using HRM.Backend.Dtos.Applications;
using HRM.Backend.Models;
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
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ApplicationsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db; _env = env;
        }
        [Authorize]
        [HttpPost]
        [RequestSizeLimit(20_000_000)] // 20 MB
        public async Task<IActionResult> Apply([FromForm] ApplyRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == req.JobId);
            if (job == null) return BadRequest("Invalid JobId.");

            // Validate resume
            var allowed = new[] { ".pdf", ".doc", ".docx" };
            var ext = Path.GetExtension(req.Resume.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return BadRequest("Resume must be PDF or Word.");
            if (req.Resume.Length <= 0 || req.Resume.Length > 10_000_000)
                return BadRequest("Resume size must be 1 byte to 10 MB.");

            // Save file under wwwroot/uploads/resumes
            var root = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var folder = Path.Combine(root, "uploads", "resumes");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, fileName);
            using (var fs = System.IO.File.Create(path))
                await req.Resume.CopyToAsync(fs);
            var publicUrl = $"/uploads/resumes/{fileName}";

            // If candidate logged in with JWT, capture their id (optional)
            string? candidateUserId = User?.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
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
                ApplicantPhone = req.Phone
            };

            _db.Applications.Add(app);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = app.Id }, new { app.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var a = await _db.Applications.Include(x => x.Job).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            return Ok(new
            {
                a.Id,
                a.JobId,
                a.AppliedOn,
                a.Status,
                a.ResumeUrl,
                a.ApplicantFullName,
                a.ApplicantEmail,
                a.ApplicantPhone
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
                .Select(a => new {
                    id = a.Id,
                    jobId = a.JobId,
                    jobTitle = a.Job!.Title,
                    appliedOn = a.AppliedOn,
                    status = a.Status.ToString(),
                    resumeUrl = a.ResumeUrl
                })
                .ToListAsync();

            return Ok(new { page, size, total, items });
        }

    }
}
