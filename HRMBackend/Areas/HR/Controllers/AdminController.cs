using HRM.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HRMBackend.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(AuthenticationSchemes = "HR", Roles = "HR")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _users;
        private readonly RoleManager<IdentityRole> _roles;
        public AdminController(UserManager<AppUser> users, RoleManager<IdentityRole> roles)
        { _users = users; _roles = roles; }

        [HttpGet]
        public IActionResult CreateHr() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHr(string email, string fullName, string password)
        {
            if (!await _roles.RoleExistsAsync("HR"))
                await _roles.CreateAsync(new IdentityRole("HR"));

            var user = new AppUser { UserName = email, Email = email, FullName = fullName };
            var res = await _users.CreateAsync(user, password);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
                return View();
            }
            await _users.AddToRoleAsync(user, "HR");
            TempData["Msg"] = "HR user created.";
            return RedirectToAction(nameof(CreateHr));
        }
    }
}
