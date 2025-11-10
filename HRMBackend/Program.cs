using HRM.Backend.Data;
using HRM.Backend.Models;
using HRM.Backend.Services;
using HRMBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------- DB ----------
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Identity ----------
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

// ---------- JWT ----------
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new Exception("Jwt:Key missing (set in Azure App Service → Configuration).");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// Auth: default to JWT for APIs; HR Razor uses explicit cookie scheme name "HR"
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

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", p =>
        p.SetIsOriginAllowed(origin =>
        {
            // local dev
            if (origin == "http://localhost:3000") return true;

            // your production vercel domain (NO trailing slash)
            if (origin == "https://hrm-app-ten.vercel.app") return true;

            // allow any vercel preview domain
            try
            {
                var host = new Uri(origin).Host; // e.g. proj-abc123-user.vercel.app
                if (host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }

            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
    // .AllowCredentials() // only if you’re using cookies from the browser
    );
});

// ---------- MVC / API ----------
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------- App Services (DI) ----------
// IMPORTANT: register ONCE with correct lifetimes
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailSenderEx, SmtpEmailSender>();           // was duplicate (Scoped + Singleton) → keep Scoped
builder.Services.AddScoped<IShortlistScorer, KeywordShortlistScorer>();  // ok as Scoped
builder.Services.AddSingleton<IVirusScanner, NoopScanner>();             // stateless → Singleton ok

var app = builder.Build();

// ---------- DB migrate ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// ---------- Seed HR role/admin (do this ONCE here; remove duplicates elsewhere) ----------
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

// HR area (Razor)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// APIs
app.MapControllers();

app.Run();
