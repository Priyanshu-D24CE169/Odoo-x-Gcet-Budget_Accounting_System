using Gcet.Models;

namespace Gcet.Services
{
    /// <summary>
    /// Password hashing contract so PBKDF2 logic is injectable and testable.
    /// </summary>
    public interface IPasswordHasherService
    {
        string HashPassword(User user, string password);
        bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword);
    }
}
