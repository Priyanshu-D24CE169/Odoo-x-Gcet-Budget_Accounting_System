using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShivFurnitureERP.Options;

namespace ShivFurnitureERP.Services;

public class GmailEmailNotificationService : IEmailNotificationService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<GmailEmailNotificationService> _logger;

    public GmailEmailNotificationService(IOptions<SmtpOptions> options, ILogger<GmailEmailNotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendContactInviteAsync(string email, string loginId, string temporaryPassword, string loginUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Cannot send invite email because no recipient email was provided.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.UserName) || string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogWarning("SMTP credentials are missing. Configure Smtp:UserName and Smtp:Password in appsettings.json.");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var fromAddress = string.IsNullOrWhiteSpace(_options.From) ? _options.UserName : _options.From;
        var normalizedLoginUrl = NormalizeUrl(loginUrl);
        var body = BuildBody(loginId, temporaryPassword, normalizedLoginUrl);

        using var message = new MailMessage(fromAddress, email)
        {
            Subject = "Portal access instructions",
            Body = body,
            IsBodyHtml = true
        };

        using var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.UserName, _options.Password)
        };

        try
        {
            await smtpClient.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Invite email successfully sent to {Email}.", email);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending invite email to {Email}. Host: {Host}, Port: {Port}, EnableSsl: {EnableSsl}", email, _options.Host, _options.Port, _options.EnableSsl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending invite email to {Email}.", email);
        }
    }

    private string BuildBody(string loginId, string password, string loginUrl)
    {
        var safeLoginUrl = string.IsNullOrWhiteSpace(loginUrl) ? "https://localhost:5001" : loginUrl;
        return $"""
            <p>Hello,</p>
            <p>Your account for Shiv Furniture ERP has been created.</p>
            <ul>
                <li><strong>Login ID:</strong> {loginId}</li>
                <li><strong>Temporary Password:</strong> {password}</li>
            </ul>
            <p>You can sign in by visiting <a href=\"{safeLoginUrl}\">{safeLoginUrl}</a>. For security, change your password after the first login.</p>
            <p>Regards,<br/>Shiv Furniture ERP</p>
        """;
    }

    private string NormalizeUrl(string loginUrl)
    {
        if (Uri.TryCreate(loginUrl, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl) && Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            if (Uri.TryCreate(baseUri, loginUrl, out var combined))
            {
                return combined.ToString();
            }
        }

        return loginUrl;
    }
}
