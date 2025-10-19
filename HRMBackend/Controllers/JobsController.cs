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
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<JobResponse>>> GetAll()
        {
            var jobs = await _db.Jobs
                .OrderByDescending(j => j.PostedOn)
                .Select(j => new JobResponse
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    Department = j.Department,
                    Location = j.Location,
                    EmploymentType = j.EmploymentType,
                    PostedOn = j.PostedOn
                })
                .ToListAsync();

            return Ok(jobs);
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
