using System.Security.Claims;
using HRM.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

namespace HRMBackend.Areas.HR.Controllers
{
    [Area("HR")]
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signIn;
        private readonly UserManager<AppUser> _users;

        public AccountController(SignInManager<AppUser> signIn, UserManager<AppUser> users)
        {
            _signIn = signIn; _users = users;
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            var user = await _users.FindByEmailAsync(email);
            if (user == null || !(await _users.CheckPasswordAsync(user, password)))
            {
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            var roles = await _users.GetRolesAsync(user);
            if (!roles.Contains("HR"))
            {
                ViewBag.Error = "Access denied (HR only).";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.Email ?? "")
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "HR");
            await HttpContext.SignInAsync("HR", new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

            if (!string.IsNullOrWhiteSpace(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard", new { area = "HR" });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("HR");
            return RedirectToAction("Login");
        }

        [HttpGet, AllowAnonymous]
        public IActionResult Denied() => View();
    }
}
