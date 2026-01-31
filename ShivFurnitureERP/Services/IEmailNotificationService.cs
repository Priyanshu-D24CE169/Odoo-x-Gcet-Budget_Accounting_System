using System.Threading;
using System.Threading.Tasks;

namespace ShivFurnitureERP.Services;

public interface IEmailNotificationService
{
	Task SendContactInviteAsync(string email, string loginId, string temporaryPassword, string loginUrl, CancellationToken cancellationToken);
}
