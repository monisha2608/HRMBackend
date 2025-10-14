using HRM.Backend.Models;
using HRMBackend.Dtos;
using HRMBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HRMBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IJwtTokenService _jwt;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IJwtTokenService jwt)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwt = jwt;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterRequest req)
        {
            var existing = await _userManager.FindByEmailAsync(req.Email);
            if (existing != null)
                return BadRequest(new { error = "Email already registered." });

            var user = new AppUser
            {
                UserName = req.Email,
                Email = req.Email,
                FullName = req.FullName
            };

            var create = await _userManager.CreateAsync(user, req.Password);
            if (!create.Succeeded)
                return BadRequest(new { error = string.Join("; ", create.Errors.Select(e => e.Description)) });

            var role = string.IsNullOrWhiteSpace(req.Role) ? "Candidate" : req.Role!;
            if (!await _userManager.IsInRoleAsync(user, role))
                await _userManager.AddToRoleAsync(user, role);

            var (token, expires) = await _jwt.CreateTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName,
                Roles = roles.ToArray(),
                ExpiresAtUtc = expires
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return Unauthorized(new { error = "Invalid credentials." });

            var pass = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
            if (!pass.Succeeded)
                return Unauthorized(new { error = "Invalid credentials." });

            var (token, expires) = await _jwt.CreateTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName,
                Roles = roles.ToArray(),
                ExpiresAtUtc = expires
            });
        }
    }
}
