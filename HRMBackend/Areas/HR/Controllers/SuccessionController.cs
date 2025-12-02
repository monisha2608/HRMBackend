using HRM.Backend.Models;
using HRMBackend.Data;
using HRMBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMBackend.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(AuthenticationSchemes = "HR", Roles = "HR")]
    public class SuccessionController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SuccessionController(ApplicationDbContext db) => _db = db;

        // ---------- ROW VIEW MODEL USED BY THE INDEX VIEW ----------
        public class RowVM
        {
            public int ApplicationId { get; set; }
            public int? SuccessionId { get; set; }

            public string CandidateName { get; set; } = string.Empty;
            public string CurrentRole { get; set; } = string.Empty;
            public string JobTitle { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;

            public string? PotentialNextRole { get; set; }

            // “Ready in” (0–6 months, 6–12 months, Within 1–2 years, 2+ years)
            public string ReadyIn { get; set; } = "Within 1–2 years";

            // Readiness / Risk: Low / Medium / High
            public string Readiness { get; set; } = "Medium";
            public string RiskOfLoss { get; set; } = "Medium";

            public string? Notes { get; set; }
            public DateTime? LastUpdatedUtc { get; set; }
        }

        // ---------- GET: HR/Succession ----------
        public async Task<IActionResult> Index(string? q)
        {
            ViewData["Title"] = "Succession";

            // Base query: hired applications with Job
            var baseQuery = _db.Applications
                .Include(a => a.Job)
                .Where(a => a.Status == ApplicationStatus.Hired);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim();
                baseQuery = baseQuery.Where(a =>
                    (a.ApplicantFullName ?? "").Contains(s) ||
                    (a.ApplicantEmail ?? "").Contains(s) ||
                    (a.Job!.Title ?? "").Contains(s));
            }

            var rows = await baseQuery
                .GroupJoin(
                    _db.SuccessionRecords,
                    app => app.Id,
                    succ => succ.ApplicationId,
                    (app, succs) => new { app, succ = succs.FirstOrDefault() }
                )
                .OrderByDescending(x => x.app.AppliedOn)
                .Select(x => new RowVM
                {
                    ApplicationId = x.app.Id,
                    SuccessionId = x.succ != null ? x.succ.Id : (int?)null,

                    CandidateName = x.app.ApplicantFullName
                                    ?? x.app.ApplicantFullName
                                    ?? "—",

                    JobTitle = x.app.Job!.Title ?? "—",
                    Department = x.app.Job!.Department ?? "—",
                    Status = x.app.Status.ToString(),

                    CurrentRole = x.succ != null && !string.IsNullOrWhiteSpace(x.succ.CurrentRole)
                        ? x.succ.CurrentRole
                        : (x.app.Job!.Title ?? "—"),

                    PotentialNextRole = x.succ != null ? x.succ.PotentialNextRole : null,
                    ReadyIn = x.succ != null ? x.succ.Readiness : "Within 1–2 years",
                    Readiness = x.succ != null ? x.succ.Readiness : "Medium",
                    RiskOfLoss = x.succ != null ? x.succ.RiskOfLoss : "Medium",
                    Notes = x.succ != null ? x.succ.DevelopmentNotes : null,
                    LastUpdatedUtc = x.succ != null ? x.succ.LastUpdatedUtc : null
                })
                .ToListAsync();

            ViewBag.Query = q ?? string.Empty;
            return View(rows);
        }

        // ---------- POST: HR/Succession/Save ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            int applicationId,
            string? potentialNextRole,
            string readyIn,
            string readiness,
            string riskOfLoss,
            string? notes)
        {
            // ensure hired application exists
            var app = await _db.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (app == null || app.Status != ApplicationStatus.Hired)
            {
                TempData["Err"] = "Application not found or not hired.";
                return RedirectToAction(nameof(Index));
            }

            // find existing succession record for this application
            var existing = await _db.SuccessionRecords
                .FirstOrDefaultAsync(s => s.ApplicationId == applicationId);

            if (existing == null)
            {
                existing = new SuccessionRecord
                {
                    ApplicationId = applicationId,
                    CandidateName = app.ApplicantFullName
                                     ?? app.ApplicantFullName
                                     ?? "Unknown",
                    CurrentRole = app.Job?.Title ?? "Unknown"
                };
                _db.SuccessionRecords.Add(existing);
            }

            existing.PotentialNextRole = string.IsNullOrWhiteSpace(potentialNextRole)
                ? null
                : potentialNextRole.Trim();

            existing.Readiness = string.IsNullOrWhiteSpace(readyIn)
                ? "Within 1–2 years"
                : readyIn;

            existing.RiskOfLoss = string.IsNullOrWhiteSpace(readiness)
                ? "Medium"
                : readiness;

            existing.RiskOfLoss = string.IsNullOrWhiteSpace(riskOfLoss)
                ? "Medium"
                : riskOfLoss;

            existing.DevelopmentNotes = string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim();

            existing.LastUpdatedUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Msg"] = "Succession record saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
