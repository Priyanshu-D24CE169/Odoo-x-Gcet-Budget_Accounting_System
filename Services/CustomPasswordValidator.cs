using Microsoft.AspNetCore.Identity;
using Budget_Accounting_System.Models;

namespace Budget_Accounting_System.Services;

public class CustomPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError { Description = "Password is required." }));
        }

        var errors = new List<IdentityError>();

        // Minimum 8 characters
        if (password.Length < 8)
        {
            errors.Add(new IdentityError { Description = "Password must be at least 8 characters long." });
        }

        // At least one uppercase letter
        if (!password.Any(char.IsUpper))
        {
            errors.Add(new IdentityError { Description = "Password must contain at least one uppercase letter." });
        }

        // At least one lowercase letter
        if (!password.Any(char.IsLower))
        {
            errors.Add(new IdentityError { Description = "Password must contain at least one lowercase letter." });
        }

        // At least one special character
        var specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        if (!password.Any(c => specialChars.Contains(c)))
        {
            errors.Add(new IdentityError { Description = "Password must contain at least one special character (!@#$%^&* etc)." });
        }

        return Task.FromResult(errors.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(errors.ToArray()));
    }
}
