using Microsoft.AspNetCore.Identity;

namespace HRM.Backend.Models
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}
