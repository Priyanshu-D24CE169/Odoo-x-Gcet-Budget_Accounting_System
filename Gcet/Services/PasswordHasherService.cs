using Gcet.Models;
using Microsoft.AspNetCore.Identity;

namespace Gcet.Services
{
    /// <summary>
    /// Wraps ASP.NET Core Identity PasswordHasher to ensure PBKDF2 with per-user salt.
    /// </summary>
    public class PasswordHasherService : IPasswordHasherService
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        public string HashPassword(User user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }

        public bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
