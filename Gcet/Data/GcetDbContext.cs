using Gcet.Models;
using Microsoft.EntityFrameworkCore;

namespace Gcet.Data
{
    /// <summary>
    /// EF Core context bound to SQL Server dbo.User table only. Parameterized queries protect against SQL injection.
    /// </summary>
    public class GcetDbContext : DbContext
    {
        public GcetDbContext(DbContextOptions<GcetDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Username)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(u => u.PasswordHash)
                    .IsRequired();

                entity.Property(u => u.Role)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("User");

                entity.Property(u => u.IsActive)
                    .HasDefaultValue(true);

                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(u => u.FailedLoginAttempts)
                    .HasDefaultValue(0);

                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Role);
                entity.HasIndex(u => u.IsActive);
            });
        }
    }
}
