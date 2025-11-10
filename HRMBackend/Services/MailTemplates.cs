using System.Net;

namespace HRM.Backend.Services
{
    public static class MailTemplates
    {
        public static string ApplicationReceived(string name, string jobTitle) =>
$@"<p>Hi {WebUtility.HtmlEncode(name)},</p>
<p>Thanks for applying to <strong>{WebUtility.HtmlEncode(jobTitle)}</strong>. Our team will review your application.</p>
<p>— XYZ HR</p>";

        public static string StatusChanged(string name, string jobTitle, string status) =>
$@"<p>Hi {WebUtility.HtmlEncode(name)},</p>
<p>Your application for <strong>{WebUtility.HtmlEncode(jobTitle)}</strong> is now: <strong>{WebUtility.HtmlEncode(status)}</strong>.</p>
<p>— XYZ HR</p>";
    }
}
