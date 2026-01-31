using System.Threading.Tasks;

namespace Gcet.Services
{
    /// <summary>
    /// Contract for sending transactional emails such as credential notifications.
    /// </summary>
    public interface IEmailService
    {
        Task SendLoginDetailsAsync(string recipientEmail, string recipientName, string username, string temporaryUserId, string temporaryPassword);
    }
}
