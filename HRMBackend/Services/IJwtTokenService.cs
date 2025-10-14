using HRM.Backend.Models;

namespace HRMBackend.Services
{
    public interface IJwtTokenService
    {
        Task<(string token, DateTime expiresUtc)> CreateTokenAsync(AppUser user);
    }
}
