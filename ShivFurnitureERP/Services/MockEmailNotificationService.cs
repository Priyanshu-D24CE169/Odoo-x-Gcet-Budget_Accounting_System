using Microsoft.Extensions.Logging;

namespace ShivFurnitureERP.Services;

public class MockEmailNotificationService : IEmailNotificationService
{
	private readonly ILogger<MockEmailNotificationService> _logger;

	public MockEmailNotificationService(ILogger<MockEmailNotificationService> logger)
	{
		_logger = logger;
	}

	public Task SendContactInviteAsync(string email, string loginId, string temporaryPassword, string loginUrl, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Mock invite email sent to {Email}. LoginId: {LoginId}, TemporaryPassword: {Password}, LoginUrl: {Url}", email, loginId, temporaryPassword, loginUrl);
		return Task.CompletedTask;
	}
}
