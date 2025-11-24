using HRMBackend.Data;
using HRMBackend.Dtos.Onboarding;
using HRMBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMBackend.Controllers
{
    [ApiController]
    [Route("api/onboarding")]
    [Authorize(Roles = "HR")] // HR-only feature
    public class OnboardingController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public OnboardingController(ApplicationDbContext db) => _db = db;

        // GET /api/onboarding/plans?q=&page=1&size=20
        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans(
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
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
                    progress = p.Tasks.Count == 0
                        ? 0
                        : (int)Math.Round(
                            100.0 * p.Tasks.Count(t => t.IsCompleted) / p.Tasks.Count
                          )
                })
                .ToListAsync();

            return Ok(new { page, size, total, items });
        }

        // POST /api/onboarding/plans
        [HttpPost("plans")]
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
                tasks = p.Tasks
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.AssignedTo,
                        t.DueDate,
                        t.IsCompleted,
                        t.CompletedOn
                    })
                    .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                    .ThenBy(t => t.Name)
            });
        }

        // POST /api/onboarding/plans/{id}/tasks
        [HttpPost("plans/{id:int}/tasks")]
        public async Task<IActionResult> AddTask(int id, [FromBody] AddTaskRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var plan = await _db.OnboardingPlans.FirstOrDefaultAsync(x => x.Id == id);
            if (plan == null) return NotFound();

            var t = new OnboardingTask
            {
                PlanId = plan.Id,
                Name = req.Name.Trim(),
                AssignedTo = string.IsNullOrWhiteSpace(req.AssignedTo)
                    ? null
                    : req.AssignedTo.Trim(),
                DueDate = req.DueDate
            };

            _db.OnboardingTasks.Add(t);
            await _db.SaveChangesAsync();

            return Created($"/api/onboarding/tasks/{t.Id}", new { t.Id });
        }

        // PATCH /api/onboarding/tasks/{taskId}
        [HttpPatch("tasks/{taskId:int}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] UpdateTaskRequest req)
        {
            var t = await _db.OnboardingTasks.FirstOrDefaultAsync(x => x.Id == taskId);
            if (t == null) return NotFound();

            if (req.Name != null)
                t.Name = req.Name.Trim();

            if (req.AssignedTo != null)
                t.AssignedTo = string.IsNullOrWhiteSpace(req.AssignedTo)
                    ? null
                    : req.AssignedTo.Trim();

            if (req.DueDate.HasValue)
                t.DueDate = req.DueDate;

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
