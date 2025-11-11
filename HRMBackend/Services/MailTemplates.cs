using System.Net;

namespace HRM.Backend.Services
{
    public static class MailTemplates
    {
        // --- Application Received ---
        public static string ApplicationReceived(string name, string jobTitle) =>
$@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            background-color: #f8fafc;
            color: #111827;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.05);
            padding: 30px;
        }}
        .header {{
            font-size: 1.3rem;
            font-weight: 600;
            color: #2563eb;
            margin-bottom: 20px;
        }}
        .footer {{
            margin-top: 30px;
            font-size: 0.9rem;
            color: #6b7280;
            border-top: 1px solid #e5e7eb;
            padding-top: 10px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>Application Received – {WebUtility.HtmlEncode(jobTitle)}</div>
        <p>Hi {WebUtility.HtmlEncode(name)},</p>
        <p>Thank you for applying for the <strong>{WebUtility.HtmlEncode(jobTitle)}</strong> position at XYZ Corporation. 
        We’ve successfully received your application and our hiring team will review it shortly.</p>
        <p>If your qualifications match our requirements, we’ll be in touch to schedule the next steps.</p>
        <p>We appreciate your interest in joining our team.</p>

        <div class='footer'>— The XYZ HR Team<br/>XYZ Corporation</div>
    </div>
</body>
</html>";

        // --- Application Status Changed ---
        public static string StatusChanged(string name, string jobTitle, string status) =>
$@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            background-color: #f8fafc;
            color: #111827;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.05);
            padding: 30px;
        }}
        .status {{
            background: #e0f2fe;
            color: #0369a1;
            padding: 10px 14px;
            border-radius: 6px;
            display: inline-block;
            font-weight: 600;
            margin: 10px 0;
        }}
        .footer {{
            margin-top: 30px;
            font-size: 0.9rem;
            color: #6b7280;
            border-top: 1px solid #e5e7eb;
            padding-top: 10px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h2 style='color:#2563eb; margin-bottom:10px;'>Application Update</h2>
        <p>Hi {WebUtility.HtmlEncode(name)},</p>
        <p>Your application for <strong>{WebUtility.HtmlEncode(jobTitle)}</strong> has been updated. The new status is:</p>
        <div class='status'>{WebUtility.HtmlEncode(status)}</div>
        <p>If you have any questions or would like to follow up, feel free to reply to this email.</p>

        <div class='footer'>— The XYZ HR Team<br/>XYZ Corporation</div>
    </div>
</body>
</html>";
    }
}
