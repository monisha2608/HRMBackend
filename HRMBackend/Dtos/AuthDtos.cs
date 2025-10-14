namespace HRMBackend.Dtos
{
    public class RegisterRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? FullName { get; set; }
        
        public string? Role { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? FullName { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public DateTime ExpiresAtUtc { get; set; }
    }
}
