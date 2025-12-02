using System.Security.Claims;
using HRMBackend.Data;
using HRM.Backend.Models;   // for ApplicationStatus
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMBackend.Controllers
{
    [ApiController]
    [Route("api/candidate/onboarding")]
    // Use the JWT Bearer scheme (or just [Authorize] if Bearer is the default)
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CandidateOnboardingController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public CandidateOnboardingController(ApplicationDbContext db) => _db = db;

        // GET /api/candidate/onboarding/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyPlan()
        {
            // Try to get the candidate's email from claims
            var candidateEmail =
                User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(candidateEmail))
                return Unauthorized("Missing candidate email in token.");

            // Find the most recent Hired application for this email
            var app = await _db.Applications
                .Where(a => a.ApplicantEmail == candidateEmail &&
                            a.Status == ApplicationStatus.Hired)
                .OrderByDescending(a => a.AppliedOn)
                .FirstOrDefaultAsync();

            if (app == null)
                return Ok(null); // no hired app yet → no onboarding plan

            var plan = await _db.OnboardingPlans
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.ApplicationId == app.Id);

            if (plan == null)
                return Ok(null); // hired but HR hasn't created a plan

            var totalTasks = plan.Tasks.Count;
            var completed = plan.Tasks.Count(t => t.IsCompleted);
            var progress = totalTasks == 0
                ? 0
                : (int)(100.0 * completed / totalTasks);

            return Ok(new
            {
                plan.Id,
                plan.CandidateName,
                plan.StartDate,
                progress,
                tasks = plan.Tasks.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.AssignedTo,
                    t.DueDate,
                    t.IsCompleted
                })
            });
        }
    }
}
