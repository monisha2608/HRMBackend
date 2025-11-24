using HRMBackend.Data;
using HRM.Backend.Models;
using HRMBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMBackend.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(AuthenticationSchemes = "HR", Roles = "HR")]
    public class OnboardingController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OnboardingController(ApplicationDbContext db) => _db = db;

        // ---- View models ----
        public class PlanRow
        {
            public int Id { get; set; }
            public string CandidateName { get; set; } = string.Empty;
            public int? ApplicationId { get; set; }
            public DateTime StartDate { get; set; }
            public int TotalTasks { get; set; }
            public int CompletedTasks { get; set; }
            public int Progress { get; set; }
        }

        public class PlanDetailsVM
        {
            public OnboardingPlan Plan { get; set; } = null!;
            public List<OnboardingTask> Tasks { get; set; } = new();
        }

        public class HiredCandidateOption
        {
            public int ApplicationId { get; set; }
            public string Label { get; set; } = string.Empty;
        }

        // helper: load hired candidates list for dropdown
        private async Task<List<HiredCandidateOption>> LoadHiredOptionsAsync()
        {
            return await _db.Applications
                .AsNoTracking()
                .Include(a => a.Job)
                .Where(a => a.Status == ApplicationStatus.Hired)
                .OrderByDescending(a => a.AppliedOn)
                .Select(a => new HiredCandidateOption
                {
                    ApplicationId = a.Id,
                    Label =
                        (a.ApplicantFullName ?? a.ApplicantEmail ?? "Unnamed candidate")
                        + " – "
                        + (a.Job!.Title ?? $"Job #{a.JobId}")
                })
                .ToListAsync();
        }

        // GET: HR/Onboarding
        public async Task<IActionResult> Index(string? q)
        {
            ViewData["Title"] = "Onboarding";

            var query = _db.OnboardingPlans
                .Include(p => p.Tasks)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim();
                query = query.Where(p => p.CandidateName.Contains(s));
            }

            var rows = await query
                .OrderByDescending(p => p.StartDate)
                .Select(p => new PlanRow
                {
                    Id = p.Id,
                    CandidateName = p.CandidateName,
                    ApplicationId = p.ApplicationId,
                    StartDate = p.StartDate,
                    TotalTasks = p.Tasks.Count,
                    CompletedTasks = p.Tasks.Count(t => t.IsCompleted),
                    Progress = p.Tasks.Count == 0
                        ? 0
                        : (int)Math.Round(
                            100.0 * p.Tasks.Count(t => t.IsCompleted) / p.Tasks.Count
                          )
                })
                .ToListAsync();

            ViewBag.Query = q ?? string.Empty;
            return View(rows);
        }

        // GET: HR/Onboarding/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? applicationId)
        {
            ViewData["Title"] = "New Onboarding Plan";

            var model = new OnboardingPlan
            {
                ApplicationId = applicationId,
                StartDate = DateTime.UtcNow
            };

            // If applicationId is provided, try to prefill candidate name
            if (applicationId.HasValue)
            {
                var app = await _db.Applications
                    .AsNoTracking()
                    .Include(a => a.Job)
                    .FirstOrDefaultAsync(a => a.Id == applicationId.Value);

                if (app != null)
                {
                    model.CandidateName = app.ApplicantFullName
                        ?? app.ApplicantEmail
                        ?? model.CandidateName;
                }
            }

            ViewBag.HiredCandidates = await LoadHiredOptionsAsync();
            return View(model);
        }

        // POST: HR/Onboarding/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OnboardingPlan model)
        {
            ViewData["Title"] = "New Onboarding Plan";

            // extra validation: onboarding only for Hired applications
            if (model.ApplicationId.HasValue)
            {
                var app = await _db.Applications
                    .AsNoTracking()
                    .Include(a => a.Job)
                    .FirstOrDefaultAsync(a => a.Id == model.ApplicationId.Value);

                if (app == null)
                {
                    ModelState.AddModelError(nameof(model.ApplicationId), "Invalid application selected.");
                }
                else if (app.Status != ApplicationStatus.Hired)
                {
                    ModelState.AddModelError(nameof(model.ApplicationId),
                        "Onboarding plans can only be created for Hired candidates.");
                }
                else
                {
                    // If candidate name is empty, fill from application
                    if (string.IsNullOrWhiteSpace(model.CandidateName))
                    {
                        model.CandidateName = app.ApplicantFullName
                            ?? app.ApplicantEmail
                            ?? "Hired candidate";
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                // repopulate dropdown on error
                ViewBag.HiredCandidates = await LoadHiredOptionsAsync();
                return View(model);
            }

            var plan = new OnboardingPlan
            {
                ApplicationId = model.ApplicationId,
                CandidateName = model.CandidateName.Trim(),
                StartDate = model.StartDate == default ? DateTime.UtcNow : model.StartDate
            };

            _db.OnboardingPlans.Add(plan);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Onboarding plan created.";
            return RedirectToAction(nameof(Details), new { id = plan.Id });
        }

        // GET: HR/Onboarding/Details/5
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Onboarding";

            var plan = await _db.OnboardingPlans
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null) return NotFound();

            var vm = new PlanDetailsVM
            {
                Plan = plan,
                Tasks = plan.Tasks
                    .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                    .ThenBy(t => t.Name)
                    .ToList()
            };

            return View(vm);
        }

        // POST: HR/Onboarding/AddTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(
            int planId,
            string name,
            string? assignedTo,
            DateTime? dueDate)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Err"] = "Task name is required.";
                return RedirectToAction(nameof(Details), new { id = planId });
            }

            var planExists = await _db.OnboardingPlans.AnyAsync(p => p.Id == planId);
            if (!planExists) return NotFound();

            var task = new OnboardingTask
            {
                PlanId = planId,
                Name = name.Trim(),
                AssignedTo = string.IsNullOrWhiteSpace(assignedTo)
                    ? null
                    : assignedTo.Trim(),
                DueDate = dueDate
            };

            _db.OnboardingTasks.Add(task);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Task added.";
            return RedirectToAction(nameof(Details), new { id = planId });
        }

        // POST: HR/Onboarding/ToggleTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTask(int taskId, int planId, bool isCompleted)
        {
            var task = await _db.OnboardingTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.PlanId == planId);

            if (task == null) return NotFound();

            task.IsCompleted = isCompleted;
            task.CompletedOn = isCompleted ? DateTime.UtcNow : null;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = planId });
        }

        // POST: HR/Onboarding/DeleteTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int taskId, int planId)
        {
            var task = await _db.OnboardingTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.PlanId == planId);

            if (task == null) return NotFound();

            _db.OnboardingTasks.Remove(task);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Task deleted.";
            return RedirectToAction(nameof(Details), new { id = planId });
        }

        // POST: HR/Onboarding/DeletePlan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _db.OnboardingPlans
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null) return NotFound();

            _db.OnboardingTasks.RemoveRange(plan.Tasks);
            _db.OnboardingPlans.Remove(plan);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Onboarding plan deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
