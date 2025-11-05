using HRM.Backend.Data;
using HRM.Backend.Models;
using HRMBackend.Data;
using HRMBackend.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public JobsController(ApplicationDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET /api/jobs
        [HttpGet]
        public async Task<IActionResult> Get(
        [FromQuery] string? q,
        [FromQuery] string? location,
        [FromQuery] string? department,
        [FromQuery] string? employmentType,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
        {
            page = page < 1 ? 1 : page;
            size = size is < 1 or > 100 ? 10 : size;

            var query = _db.Jobs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var like = $"%{q.Trim()}%";
                query = query.Where(j =>
                    EF.Functions.Like(j.Title, like) ||
                    EF.Functions.Like(j.Description!, like));
            }
            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(j => j.Location == location);

            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(j => j.Department == department);

            if (!string.IsNullOrWhiteSpace(employmentType))
                query = query.Where(j => j.EmploymentType == employmentType);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(j => j.PostedOn)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(j => new {
                    id = j.Id,
                    title = j.Title,
                    description = j.Description,
                    department = j.Department,
                    location = j.Location,
                    employmentType = j.EmploymentType,
                    postedOn = j.PostedOn
                })
                .ToListAsync();

            return Ok(new { page, size, total, items });
        }


        // GET /api/jobs/{id}
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<JobResponse>> GetById(int id)
        {
            var j = await _db.Jobs.FindAsync(id);
            if (j == null) return NotFound();

            return Ok(new JobResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Department = j.Department,
                Location = j.Location,
                EmploymentType = j.EmploymentType,
                PostedOn = j.PostedOn
            });
        }

        // POST /api/jobs  (HR only)
        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult<JobResponse>> Create(JobCreateRequest req)
        {
            var userId = _userManager.GetUserId(User)!;

            var job = new Job
            {
                Title = req.Title,
                Description = req.Description,
                Department = req.Department,
                Location = req.Location,
                EmploymentType = req.EmploymentType,
                PostedOn = DateTime.UtcNow,
                PostedByUserId = userId
            };

            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();

            var resp = new JobResponse
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                Department = job.Department,
                Location = job.Location,
                EmploymentType = job.EmploymentType,
                PostedOn = job.PostedOn
            };

            return CreatedAtAction(nameof(GetById), new { id = job.Id }, resp);
        }

        // PUT /api/jobs/{id}  (HR only)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Update(int id, JobUpdateRequest req)
        {
            var job = await _db.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            job.Title = req.Title;
            job.Description = req.Description;
            job.Department = req.Department;
            job.Location = req.Location;
            job.EmploymentType = req.EmploymentType;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/jobs/{id}  (HR only)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Delete(int id)
        {
            var job = await _db.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            _db.Jobs.Remove(job);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
