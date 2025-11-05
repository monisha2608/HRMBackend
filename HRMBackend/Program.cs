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
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key missing");
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
        p.WithOrigins("http://localhost:3000", "https://<your-vercel-app>.vercel.app")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
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
