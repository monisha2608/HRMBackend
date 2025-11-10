using HRM.Backend.Data;
using HRM.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMBackend.Areas.HR.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMBackend.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(AuthenticationSchemes = "HR", Roles = "HR")]
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public JobsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? q)
        {
            var jobs = _db.Jobs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                jobs = jobs.Where(j =>
                    EF.Functions.Like(j.Title, $"%{q}%") ||
                    EF.Functions.Like(j.Department!, $"%{q}%") ||
                    EF.Functions.Like(j.Location!, $"%{q}%"));
            }

            var list = await jobs
                .OrderByDescending(j => j.PostedOn)
                .Select(j => new JobRow
                {
                    Id = j.Id,
                    Title = j.Title,
                    Department = j.Department,
                    Location = j.Location,
                    EmploymentType = j.EmploymentType,
                    PostedOn = j.PostedOn,
                    Applicants = _db.Applications.Count(a => a.JobId == j.Id)
                }).ToListAsync();

            ViewBag.Query = q;
            return View(list);
        }

        [HttpGet]
        public IActionResult Create() => View(new Job());
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Department,Location,EmploymentType")] Job model)
        {
            // Server-only fields and nav property should not block validation
            ModelState.Remove(nameof(Job.PostedByUserId));
            ModelState.Remove(nameof(Job.PostedOn));
            ModelState.Remove(nameof(Job.PostedByUser));  // <-- ignore nav
            ModelState.Remove("PostedByUser");            // <-- belt + suspenders

            if (string.IsNullOrWhiteSpace(model.Title))
                ModelState.AddModelError(nameof(Job.Title), "Title is required.");
            if (string.IsNullOrWhiteSpace(model.Description))
                ModelState.AddModelError(nameof(Job.Description), "Description is required.");

            var uid = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
                ModelState.AddModelError("", "Cannot resolve current HR user. Please sign out and in again.");

            if (!ModelState.IsValid) return View(model);

            // Set server fields
            model.PostedByUserId = uid!;
            model.PostedOn = DateTime.UtcNow;
            model.PostedByUser = null;  // <-- important if the nav is [Required] or non-nullable

            _db.Jobs.Add(model);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Job created successfully.";
            return RedirectToAction(nameof(Index));
        }



        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var job = await _db.Jobs.FindAsync(id);
            if (job == null) return NotFound();
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Job model)
        {
            ModelState.Remove(nameof(Job.PostedByUserId));
            ModelState.Remove(nameof(Job.PostedByUser));
            ModelState.Remove(nameof(Job.PostedOn));
            var job = await _db.Jobs.FindAsync(id);
            if (job == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            job.Title = model.Title;
            job.Description = model.Description;
            job.Department = model.Department;
            job.Location = model.Location;
            job.EmploymentType = model.EmploymentType;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return NotFound();

            _db.Jobs.Remove(job);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Job deleted.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Applicants(int id)
            => RedirectToAction("Index", "Applications", new { area = "HR", jobId = id });

        public class JobRow
        {
            public int Id { get; set; }
            public string Title { get; set; } = default!;
            public string? Department { get; set; }
            public string? Location { get; set; }
            public string? EmploymentType { get; set; }
            public DateTime PostedOn { get; set; }
            public int Applicants { get; set; }
        }
    }
}
