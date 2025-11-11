using System.Net;
using System.Net.Mail;

namespace HRM.Backend.Services
{
    public class SmtpEmailSender : IEmailSenderEx
    {
        private readonly IConfiguration _cfg;
        public SmtpEmailSender(IConfiguration cfg) { _cfg = cfg; }

        public async Task SendAsync(string to, string subject, string html)
        {
            var host = _cfg["Smtp:Host"];
            var port = int.Parse(_cfg["Smtp:Port"] ?? "587");
            var user = _cfg["Smtp:User"];
            var pass = _cfg["Smtp:Pass"];
            var from = _cfg["Mail:From"];
            var fromName = _cfg["Mail:FromName"] ?? "HR";

            using var msg = new MailMessage();
            msg.From = new MailAddress(from!, fromName);
            msg.To.Add(to);
            msg.Subject = subject;
            msg.Body = html;
            msg.IsBodyHtml = true;

            using var smtp = new SmtpClient(host!, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass)
            };
            await smtp.SendMailAsync(msg);
        }
    }
}
