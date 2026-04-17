using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EmployeeTracker.API.Services
{
    public interface IEmailService
    {
        Task SendPasswordEmailAsync(string toEmail, string toName, string password);
        Task SendPasswordResetEmailAsync(string toEmail, string toName, string newPassword);
        Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendPasswordEmailAsync(string toEmail, string toName, string password)
        {
            var subject = "Welcome to EmployeeTracker — Your Account is Ready";
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
  <title>Welcome to EmployeeTracker</title>
  <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'/>
</head>
<body style='margin:0;padding:0;background-color:#EFF4FF;font-family:Inter,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 16px;'>
    <tr><td align='center'>
      <table width='100%' style='max-width:560px;background:#ffffff;border-radius:20px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

        <!-- Header -->
        <tr><td style='background:linear-gradient(135deg,#1D4ED8 0%,#2563EB 60%,#3B82F6 100%);padding:36px 40px 28px;text-align:center;'>
          <div style='display:inline-flex;align-items:center;justify-content:center;width:52px;height:52px;background:rgba(255,255,255,0.2);border-radius:14px;margin-bottom:16px;'>
            <span style='color:#fff;font-size:20px;font-weight:800;letter-spacing:1px;'>ET</span>
          </div>
          <div style='color:#fff;font-size:22px;font-weight:700;letter-spacing:-0.5px;margin-bottom:6px;'>Welcome to EmployeeTracker</div>
          <div style='color:rgba(255,255,255,0.75);font-size:14px;'>Your account has been set up and is ready to use</div>
        </td></tr>

        <!-- Body -->
        <tr><td style='padding:36px 40px;'>
          <p style='margin:0 0 8px;font-size:16px;font-weight:600;color:#0F172A;'>Hi {toName},</p>
          <p style='margin:0 0 28px;font-size:14px;color:#64748B;line-height:1.7;'>HR has created your EmployeeTracker account. Use the credentials below to sign in for the first time.</p>

          <!-- Credentials Box -->
          <div style='background:#F8FAFF;border:1px solid #DDE5F5;border-radius:14px;padding:24px;margin-bottom:28px;'>
            <div style='margin-bottom:18px;'>
              <div style='font-size:11px;font-weight:700;letter-spacing:1.2px;color:#94A3B8;text-transform:uppercase;margin-bottom:6px;'>Email Address</div>
              <div style='background:#fff;border:1px solid #E2E8F0;border-radius:10px;padding:12px 16px;font-size:15px;font-weight:600;color:#1E40AF;font-family:monospace;'>{toEmail}</div>
            </div>
            <div>
              <div style='font-size:11px;font-weight:700;letter-spacing:1.2px;color:#94A3B8;text-transform:uppercase;margin-bottom:6px;'>Temporary Password</div>
              <div style='background:#fff;border:1px solid #E2E8F0;border-radius:10px;padding:12px 16px;font-size:15px;font-weight:600;color:#1E40AF;font-family:monospace;letter-spacing:1px;'>{password}</div>
            </div>
          </div>

          <!-- CTA -->
          <div style='background:#EFF6FF;border-left:4px solid #2563EB;border-radius:0 10px 10px 0;padding:16px 20px;margin-bottom:28px;'>
            <p style='margin:0;font-size:13px;color:#1D4ED8;line-height:1.6;'>🔒 <strong>Important:</strong> Please log in and change your password immediately after your first sign-in. Do not share your credentials with anyone.</p>
          </div>

          <p style='margin:0;font-size:13px;color:#94A3B8;line-height:1.6;'>If you have questions, contact your HR administrator.</p>
        </td></tr>

        <!-- Footer -->
        <tr><td style='background:#F8FAFC;border-top:1px solid #E2E8F0;padding:20px 40px;text-align:center;'>
          <p style='margin:0;font-size:12px;color:#94A3B8;'>© 2026 EmployeeTracker · Enterprise Edition</p>
          <p style='margin:6px 0 0;font-size:11px;color:#CBD5E1;'>This is an automated message. Please do not reply to this email.</p>
        </td></tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

            await SendEmailAsync(toEmail, toName, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string newPassword)
        {
            var subject = "EmployeeTracker — Your Password Has Been Reset";
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
  <title>Password Reset</title>
  <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'/>
</head>
<body style='margin:0;padding:0;background-color:#EFF4FF;font-family:Inter,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 16px;'>
    <tr><td align='center'>
      <table width='100%' style='max-width:560px;background:#ffffff;border-radius:20px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

        <!-- Header -->
        <tr><td style='background:linear-gradient(135deg,#7C3AED 0%,#6D28D9 60%,#5B21B6 100%);padding:36px 40px 28px;text-align:center;'>
          <div style='display:inline-flex;align-items:center;justify-content:center;width:52px;height:52px;background:rgba(255,255,255,0.2);border-radius:14px;margin-bottom:16px;'>
            <span style='color:#fff;font-size:24px;'>🔐</span>
          </div>
          <div style='color:#fff;font-size:22px;font-weight:700;letter-spacing:-0.5px;margin-bottom:6px;'>Password Reset Successful</div>
          <div style='color:rgba(255,255,255,0.75);font-size:14px;'>Your EmployeeTracker password has been updated by HR</div>
        </td></tr>

        <!-- Body -->
        <tr><td style='padding:36px 40px;'>
          <p style='margin:0 0 8px;font-size:16px;font-weight:600;color:#0F172A;'>Hi {toName},</p>
          <p style='margin:0 0 28px;font-size:14px;color:#64748B;line-height:1.7;'>Your account password has been reset by HR. Here are your updated login credentials:</p>

          <!-- Credentials Box -->
          <div style='background:#F8FAFF;border:1px solid #DDE5F5;border-radius:14px;padding:24px;margin-bottom:28px;'>
            <div style='margin-bottom:18px;'>
              <div style='font-size:11px;font-weight:700;letter-spacing:1.2px;color:#94A3B8;text-transform:uppercase;margin-bottom:6px;'>Email Address</div>
              <div style='background:#fff;border:1px solid #E2E8F0;border-radius:10px;padding:12px 16px;font-size:15px;font-weight:600;color:#5B21B6;font-family:monospace;'>{toEmail}</div>
            </div>
            <div>
              <div style='font-size:11px;font-weight:700;letter-spacing:1.2px;color:#94A3B8;text-transform:uppercase;margin-bottom:6px;'>New Password</div>
              <div style='background:#fff;border:1px solid #E2E8F0;border-radius:10px;padding:12px 16px;font-size:15px;font-weight:600;color:#5B21B6;font-family:monospace;letter-spacing:1px;'>{newPassword}</div>
            </div>
          </div>

          <!-- Warning -->
          <div style='background:#FDF4FF;border-left:4px solid #7C3AED;border-radius:0 10px 10px 0;padding:16px 20px;margin-bottom:28px;'>
            <p style='margin:0;font-size:13px;color:#6D28D9;line-height:1.6;'>🔒 <strong>Action required:</strong> Log in and change your password immediately. If you did not request this reset, contact your HR administrator right away.</p>
          </div>

          <p style='margin:0;font-size:13px;color:#94A3B8;line-height:1.6;'>For security purposes, please do not share your password with anyone.</p>
        </td></tr>

        <!-- Footer -->
        <tr><td style='background:#F8FAFC;border-top:1px solid #E2E8F0;padding:20px 40px;text-align:center;'>
          <p style='margin:0;font-size:12px;color:#94A3B8;'>© 2026 EmployeeTracker · Enterprise Edition</p>
          <p style='margin:6px 0 0;font-size:11px;color:#CBD5E1;'>This is an automated message. Please do not reply to this email.</p>
        </td></tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

            await SendEmailAsync(toEmail, toName, subject, body);
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Email:DisplayName"] ?? "EmployeeTracker",
                _config["Email:From"] ?? "noreply@company.com"
            ));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["Email:Host"],
                int.Parse(_config["Email:Port"]!),
                SecureSocketOptions.StartTls
            );
            await client.AuthenticateAsync(
                _config["Email:From"],
                _config["Email:Password"]
            );
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}