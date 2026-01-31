using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Gcet.Data;
using Gcet.Models;
using Microsoft.EntityFrameworkCore;

namespace Gcet.Services
{
    /// <summary>
    /// Handles all user persistence logic with built-in lockout and password policies.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly GcetDbContext _context;
        private readonly IPasswordHasherService _passwordHasher;
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public UserService(GcetDbContext context, IPasswordHasherService passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public Task<User?> GetByIdAsync(int id) => _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        public Task<User?> GetByUsernameOrEmailAsync(string input)
        {
            var lowered = input.Trim().ToLower();
            return _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == lowered || u.Email.ToLower() == lowered);
        }

        public Task<bool> UsernameExistsAsync(string username)
        {
            var lowered = username.Trim().ToLower();
            return _context.Users.AnyAsync(u => u.Username.ToLower() == lowered);
        }

        public Task<bool> EmailExistsAsync(string email)
        {
            var lowered = email.Trim().ToLower();
            return _context.Users.AnyAsync(u => u.Email.ToLower() == lowered);
        }

        public async Task<User> CreateAsync(User user, string password, bool markPasswordChanged = true)
        {
            user.Role = user.Role is "Admin" or "User" ? user.Role : "User";
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            user.CreatedAt = DateTime.UtcNow;
            user.PasswordChangedAt = markPasswordChanged ? DateTime.UtcNow : null;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public Task<bool> ValidatePasswordAsync(User user, string password)
            => Task.FromResult(_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password));

        public async Task RecordSuccessfulLoginAsync(User user)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RecordFailedLoginAsync(User user)
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword)
        {
            if (!_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword))
            {
                return false;
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, newPassword))
            {
                return false; // Prevent reuse of last password.
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .OrderBy(u => u.Username)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(IEnumerable<User> Users, int TotalCount)> SearchAsync(string? search, int pageNumber, int pageSize)
        {
            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.Trim().ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(lowered) || u.Email.ToLower().Contains(lowered));
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<UserStatistics> GetDashboardMetricsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var lockedUsers = await _context.Users.CountAsync(u => u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTime.UtcNow);

            return new UserStatistics
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = totalUsers - activeUsers,
                LockedUsers = lockedUsers
            };
        }

        public async Task ToggleUserStatusAsync(int userId, bool isActive)
        {
            var user = await GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found");
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserRoleAsync(int userId, string role)
        {
            if (role != "Admin" && role != "User")
            {
                throw new ArgumentException("Invalid role", nameof(role));
            }

            var user = await GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found");
            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteUserAsync(int userId)
        {
            var user = await GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found");
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task UnlockUserAsync(int userId)
        {
            var user = await GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found");
            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<string> ResetPasswordAsync(int userId)
        {
            var user = await GetByIdAsync(userId) ?? throw new InvalidOperationException("User not found");
            var temporaryPassword = GenerateSecurePassword();
            user.PasswordHash = _passwordHasher.HashPassword(user, temporaryPassword);
            user.PasswordChangedAt = null; // Forces password change at next login.
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return temporaryPassword;
        }

        private static string GenerateSecurePassword()
        {
            const string allowed = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789@$!%*?&";
            var randomBytes = new byte[16];
            RandomNumberGenerator.Fill(randomBytes);
            Span<char> buffer = stackalloc char[randomBytes.Length];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = allowed[randomBytes[i] % allowed.Length];
            }

            return new string(buffer);
        }
    }
}
