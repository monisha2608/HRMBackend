namespace HRM.Backend.Services
{
    public interface IEmailSenderEx
    {
        Task SendAsync(string to, string subject, string html);
    }
}
