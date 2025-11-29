using HRMBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMBackend.Controllers
{
    [ApiController]
    [Route("api/my-onboarding")]
    [Authorize] // <-- candidate auth (NOT HR role)
    public class MyOnboardingController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public MyOnboardingController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /api/my-onboarding/plans
        [HttpGet("plans")]
        public async Task<IActionResult> GetMyPlans(
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            page = page < 1 ? 1 : page;
            size = size is < 1 or > 100 ? 20 : size;

            // Join OnboardingPlans to Applications so we can
            // filter by the candidate user
            var query =
                from p in _db.OnboardingPlans.Include(p => p.Tasks)
                join a in _db.Applications on p.ApplicationId equals a.Id
                where a.CandidateUserId == userId
                select new
                {
                    p.Id,
                    p.CandidateName,
                    p.ApplicationId,
                    p.StartDate,
                    TotalTasks = p.Tasks.Count,
                    CompletedTasks = p.Tasks.Count(t => t.IsCompleted),
                    Progress = p.Tasks.Count == 0
                        ? 0
                        : (int)Math.Round(
                            100.0 * p.Tasks.Count(t => t.IsCompleted) / p.Tasks.Count
                          )
                };

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.StartDate)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return Ok(new { page, size, total, items });
        }
    }
}
