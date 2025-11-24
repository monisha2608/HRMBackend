using HRMBackend.Data;
using HRMBackend.Dtos.Onboarding;
using HRMBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // generic; we'll decorate HR-only actions below
    public class OnboardingController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public OnboardingController(ApplicationDbContext db) => _db = db;

        // ===================== CANDIDATE VIEW =====================

        // GET /api/onboarding/mine
        // Returns onboarding plans linked to the logged-in candidate's applications
        [HttpGet("mine")]
        [Authorize(Roles = "Candidate")] // adjust if your role name is different
        public async Task<IActionResult> Mine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Join OnboardingPlans -> Applications -> filter by CandidateUserId
            var query = from plan in _db.OnboardingPlans
                        join app in _db.Applications
                            on plan.ApplicationId equals app.Id
                        where app.CandidateUserId == userId
                        select new
                        {
                            plan.Id,
                            plan.CandidateName,
                            plan.ApplicationId,
                            plan.StartDate,
                            Tasks = plan.Tasks
                        };

            var list = await query
                .AsNoTracking()
                .ToListAsync();

            var items = list.Select(p =>
            {
                var total = p.Tasks.Count;
                var done = p.Tasks.Count(t => t.IsCompleted);
                var progress = total == 0 ? 0 : (int)Math.Round(100.0 * done / total);

                return new
                {
                    id = p.Id,
                    candidateName = p.CandidateName,
                    applicationId = p.ApplicationId,
                    startDate = p.StartDate,
                    progress,
                    totalTasks = total,
                    completedTasks = done,
                    tasks = p.Tasks
                        .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                        .ThenBy(t => t.Name)
                        .Select(t => new
                        {
                            id = t.Id,
                            name = t.Name,
                            assignedTo = t.AssignedTo,
                            dueDate = t.DueDate,
                            isCompleted = t.IsCompleted,
                            completedOn = t.CompletedOn
                        })
                };
            });

            return Ok(new { items });
        }

        // ===================== HR CONSOLE (existing stuff) =====================

        // GET /api/onboarding/plans?q=&page=1&size=20
        [HttpGet("plans")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetPlans([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            page = page < 1 ? 1 : page;
            size = size is < 1 or > 100 ? 20 : size;

            var query = _db.OnboardingPlans
                .AsNoTracking()
                .Include(p => p.Tasks)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim();
                query = query.Where(p => p.CandidateName.Contains(s));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(p => new
                {
                    p.Id,
                    p.CandidateName,
                    p.ApplicationId,
                    p.StartDate,
                    totalTasks = p.Tasks.Count,
                    completedTasks = p.Tasks.Count(t => t.IsCompleted),
                    progress = p.Tasks.Count == 0 ? 0 :
                        (int)Math.Round(100.0 * p.Tasks.Count(t => t.IsCompleted) / p.Tasks.Count)
                })
                .ToListAsync();

            return Ok(new { page, size, total, items });
        }

        // POST /api/onboarding/plans
        [HttpPost("plans")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var plan = new OnboardingPlan
            {
                ApplicationId = req.ApplicationId,
                CandidateName = req.CandidateName.Trim(),
                StartDate = req.StartDate ?? DateTime.UtcNow
            };
            _db.OnboardingPlans.Add(plan);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPlanById), new { id = plan.Id }, new { plan.Id });
        }

        // GET /api/onboarding/plans/{id}
        [HttpGet("plans/{id:int}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetPlanById(int id)
        {
            var p = await _db.OnboardingPlans
                .AsNoTracking()
                .Include(x => x.Tasks)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();

            var total = p.Tasks.Count;
            var done = p.Tasks.Count(t => t.IsCompleted);
            var progress = total == 0 ? 0 : (int)Math.Round(100.0 * done / total);

            return Ok(new
            {
                p.Id,
                p.CandidateName,
                p.ApplicationId,
                p.StartDate,
                progress,
                tasks = p.Tasks.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.AssignedTo,
                    t.DueDate,
                    t.IsCompleted,
                    t.CompletedOn
                }).OrderBy(t => t.DueDate ?? DateTime.MaxValue).ThenBy(t => t.Name)
            });
        }

        // POST /api/onboarding/plans/{id}/tasks
        [HttpPost("plans/{id:int}/tasks")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> AddTask(int id, [FromBody] AddTaskRequest req)
        {
            var plan = await _db.OnboardingPlans.FirstOrDefaultAsync(x => x.Id == id);
            if (plan == null) return NotFound();

            var t = new OnboardingTask
            {
                PlanId = plan.Id,
                Name = req.Name.Trim(),
                AssignedTo = string.IsNullOrWhiteSpace(req.AssignedTo) ? null : req.AssignedTo.Trim(),
                DueDate = req.DueDate
            };
            _db.OnboardingTasks.Add(t);
            await _db.SaveChangesAsync();

            return Created($"/api/onboarding/tasks/{t.Id}", new { t.Id });
        }

        // PATCH /api/onboarding/tasks/{taskId}
        [HttpPatch("tasks/{taskId:int}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] UpdateTaskRequest req)
        {
            var t = await _db.OnboardingTasks.FirstOrDefaultAsync(x => x.Id == taskId);
            if (t == null) return NotFound();

            if (req.Name != null) t.Name = req.Name.Trim();
            if (req.AssignedTo != null) t.AssignedTo = string.IsNullOrWhiteSpace(req.AssignedTo) ? null : req.AssignedTo.Trim();
            if (req.DueDate.HasValue) t.DueDate = req.DueDate;
            if (req.IsCompleted.HasValue)
            {
                t.IsCompleted = req.IsCompleted.Value;
                t.CompletedOn = t.IsCompleted ? DateTime.UtcNow : null;
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/onboarding/tasks/{taskId}
        [HttpDelete("tasks/{taskId:int}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var t = await _db.OnboardingTasks.FirstOrDefaultAsync(x => x.Id == taskId);
            if (t == null) return NotFound();
            _db.OnboardingTasks.Remove(t);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
