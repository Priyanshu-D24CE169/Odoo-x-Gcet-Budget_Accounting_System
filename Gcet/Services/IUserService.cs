using System.Collections.Generic;
using System.Threading.Tasks;
using Gcet.Models;

namespace Gcet.Services
{
    /// <summary>
    /// Contract for secure user operations encapsulating lockout, auditing, and role management.
    /// </summary>
    public interface IUserService
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameOrEmailAsync(string input);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<User> CreateAsync(User user, string password, bool markPasswordChanged = true);
        Task RecordSuccessfulLoginAsync(User user);
        Task RecordFailedLoginAsync(User user);
        Task<bool> ValidatePasswordAsync(User user, string password);
        Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword);
        Task<IEnumerable<User>> GetAllAsync();
        Task<(IEnumerable<User> Users, int TotalCount)> SearchAsync(string? search, int pageNumber, int pageSize);
        Task<UserStatistics> GetDashboardMetricsAsync();
        Task ToggleUserStatusAsync(int userId, bool isActive);
        Task UpdateUserRoleAsync(int userId, string role);
        Task UnlockUserAsync(int userId);
        Task<string> ResetPasswordAsync(int userId);
        Task SoftDeleteUserAsync(int userId);
    }
}
