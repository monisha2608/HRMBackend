using HRMBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMBackend.Controllers
{
    [ApiController]
    [Route("api/candidate/onboarding")]
    [Authorize] // normal candidate auth
    public class CandidateOnboardingController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public CandidateOnboardingController(ApplicationDbContext db) => _db = db;

        // GET /api/candidate/onboarding?applicationId=123
        [HttpGet]
        public async Task<IActionResult> GetMyPlan([FromQuery] int applicationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // 1) Make sure this application belongs to the logged-in candidate
            var app = await _db.Applications
                .AsNoTracking()
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a =>
                    a.Id == applicationId &&
                    a.CandidateUserId == userId);

            if (app == null)
                return NotFound("Application not found for this user.");

            // 2) Load onboarding plan + tasks for this application
            var plan = await _db.OnboardingPlans
                .Include(p => p.Tasks)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationId == applicationId);

            if (plan == null)
                return NotFound("No onboarding plan yet for this application.");

            var totalTasks = plan.Tasks.Count;
            var completed = plan.Tasks.Count(t => t.IsCompleted);
            var progress = totalTasks == 0 ? 0 : (int)Math.Round(100.0 * completed / totalTasks);

            return Ok(new
            {
                applicationId = app.Id,
                jobTitle = app.Job?.Title ?? $"Job #{app.JobId}",
                candidateName = plan.CandidateName,
                startDate = plan.StartDate,
                progress,
                totalTasks,
                completedTasks = completed,
                tasks = plan.Tasks
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
            });
        }
    }
}
