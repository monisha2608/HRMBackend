using HRM.Backend.Data;
using HRM.Backend.Models;
using HRMBackend.Data;
using HRMBackend.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// IdentityCore (you already have this)
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager<SignInManager<AppUser>>()
.AddDefaultTokenProviders();

// JWT (for APIs, used by React)
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    Console.Error.WriteLine("FATAL: Jwt:Key missing. Set it in App Service → Environment variables.");
    throw new Exception("Jwt:Key missing");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// Auth: add BOTH JwtBearer and a Cookie scheme named "HR"
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
})
.AddCookie("HR", options =>
{
    options.LoginPath = "/hr/account/login";
    options.AccessDeniedPath = "/hr/account/denied";
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// CORS for React
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", p =>
        p.SetIsOriginAllowed(origin =>
        {
            // allow localhost dev
            if (origin == "http://localhost:3000") return true;

            // allow your production vercel domain EXACTLY:
            if (origin == "https://hrm-app-ten.vercel.app/") return true;

            // allow any vercel preview domain (e.g., https://proj-abc123-user.vercel.app)
            try
            {
                var host = new Uri(origin).Host; // e.g., proj-abc123-user.vercel.app
                if (host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { /* ignore parse errors */ }

            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
    // Only needed if you use cookies; safe to leave off since you use Bearer tokens:
    //.AllowCredentials()
    );
});


// MVC + API
builder.Services.AddControllersWithViews(); // 👈 add views
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
using (var scope = app.Services.CreateScope())
{
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    if (!await roleMgr.RoleExistsAsync("HR"))
        await roleMgr.CreateAsync(new IdentityRole("HR"));

    var adminEmail = Environment.GetEnvironmentVariable("HR_ADMIN_EMAIL") ?? "hr@xyzcorp.com";
    var adminPass = Environment.GetEnvironmentVariable("HR_ADMIN_PASSWORD") ?? "HrAdmin123!";

    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new AppUser { UserName = adminEmail, Email = adminEmail, FullName = "HR Admin" };
        await userMgr.CreateAsync(admin, adminPass);
        await userMgr.AddToRoleAsync(admin, "HR");
    }
}
app.UseStaticFiles();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseCors("Frontend");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// MVC (HR area & default)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// APIs
app.MapControllers();

// Seed roles/admin
await DbSeeder.SeedAsync(app.Services);

app.Run();
