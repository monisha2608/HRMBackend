using HRM.Backend.Data;
using HRMBackend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMBackend.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(AuthenticationSchemes = "HR", Roles = "HR")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalJobs = await _db.Jobs.CountAsync();
            ViewBag.TotalApps = await _db.Applications.CountAsync();
            ViewBag.ByStatus = await _db.Applications
                .GroupBy(a => a.Status)
                .Select(g => new { k = g.Key.ToString(), c = g.Count() })
                .ToDictionaryAsync(x => x.k, x => x.c);
            return View();
        }
    }
}
