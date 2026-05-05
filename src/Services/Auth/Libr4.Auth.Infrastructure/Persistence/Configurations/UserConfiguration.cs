using Libr4.Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Auth.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.HasKey(x => x.Id);
        e.Property(x => x.Email).IsRequired().HasMaxLength(254);
        e.HasIndex(x => x.Email).IsUnique();
        e.Property(x => x.DisplayName).IsRequired().HasMaxLength(64);
        e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(256);
        e.Property(x => x.TwoFactorSecretEncrypted).HasMaxLength(512);
        e.Property(x => x.CreatedAt);
        e.Property(x => x.UpdatedAt);
        e.Property(x => x.LastLoginAt);
        e.Property(x => x.LockedOutUntil);
        e.Property(x => x.FailedLoginAttempts);
        e.Property(x => x.IsActive);
        e.Property(x => x.EmailConfirmed);
        e.Property(x => x.TwoFactorEnabled);

        // Role flags
        e.Property(x => x.IsFreelancer);
        e.Property(x => x.IsClient);
        e.Property(x => x.IsAdmin);
        e.Property(x => x.IsDeveloper);
        e.Property(x => x.IsTrader);
        e.Property(x => x.IsLearner);
        e.Property(x => x.IsSocialOnly);

        // Profile fields
        e.Property(x => x.FullName).HasMaxLength(100);
        e.Property(x => x.Bio);
        e.Property(x => x.Skills).HasConversion(
            v => string.Join(",", v),
            v => v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));
        e.Property(x => x.HourlyRate).HasColumnType("decimal(10,2)");
        e.Property(x => x.AvatarUrl).HasMaxLength(500);

        // Stats
        e.Property(x => x.Rating);
        e.Property(x => x.TotalEarnings).HasColumnType("decimal(10,2)");
        e.Property(x => x.TotalSpent).HasColumnType("decimal(10,2)");
        e.Property(x => x.CompletedTasks);

        // KYC/AML
        e.Property(x => x.KycVerified);
        e.Property(x => x.KycStatus).HasMaxLength(50);
        e.Property(x => x.AmlChecked);
        e.Property(x => x.SanctionsChecked);

        // AI matching
        e.Property(x => x.Level);
        e.Property(x => x.SkillScore);

        e.HasMany(x => x.Roles)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.RefreshTokens)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        e.Ignore(x => x.DomainEvents);
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> e)
    {
        e.ToTable("user_roles");
        e.HasKey(x => new { x.UserId, x.Role });
        e.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> e)
    {
        e.ToTable("refresh_tokens");
        e.HasKey(x => x.Id);
        e.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        e.HasIndex(x => x.TokenHash).IsUnique();
        e.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
        e.Property(x => x.CreatedByIp).HasMaxLength(64);
        e.Property(x => x.RevokedByIp).HasMaxLength(64);
    }
}

public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> e)
    {
        e.ToTable("user_tokens");
        e.HasKey(x => x.Id);
        e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        e.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        e.HasIndex(x => new { x.TokenHash, x.Kind }).IsUnique();
        e.HasIndex(x => x.ExpiresAt);
    }
}
