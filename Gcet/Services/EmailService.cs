using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Gcet.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gcet.Services
{
    /// <summary>
    /// Sends HTML emails using the configured SMTP provider.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendLoginDetailsAsync(string recipientEmail, string recipientName, string username, string temporaryUserId, string temporaryPassword)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.FromAddress))
            {
                _logger.LogWarning("SMTP settings are incomplete. Email will not be sent.");
                return;
            }

            using var smtp = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            var subject = "Your Account Login Details";
            var builder = new StringBuilder();
            builder.Append("<p>Hello ").Append(WebUtility.HtmlEncode(recipientName)).Append(",</p>");
            builder.Append("<p>Your account has been created. Please find the temporary credentials below:</p>");
            builder.Append("<ul>");
            builder.AppendFormat("<li><strong>Temporary User ID:</strong> {0}</li>", WebUtility.HtmlEncode(temporaryUserId));
            builder.AppendFormat("<li><strong>Username:</strong> {0}</li>", WebUtility.HtmlEncode(username));
            builder.AppendFormat("<li><strong>Temporary Password:</strong> {0}</li>", WebUtility.HtmlEncode(temporaryPassword));
            builder.Append("</ul>");
            builder.AppendFormat("<p>Login here: <a href=\"{0}\">{0}</a></p>", _settings.LoginUrl);
            builder.Append("<p>Please change your password immediately after signing in.</p>");
            builder.Append("<p>If you did not expect this email, contact the administrator.</p>");

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = builder.ToString(),
                IsBodyHtml = true
            };
            message.To.Add(recipientEmail);

            await smtp.SendMailAsync(message);
            _logger.LogInformation("Sent credential email to {Email}", recipientEmail);
        }
    }
}
