using Libr4.Auth.Domain.ApiKeys;
using Libr4.Auth.Domain.Gdpr;
using Libr4.Auth.Domain.Kyc;
using Libr4.Auth.Domain.Levels;
using Libr4.Auth.Domain.Onboarding;
using Libr4.Auth.Domain.Organizations;
using Libr4.Auth.Domain.Profiles;
using Libr4.Auth.Domain.Security;
using Libr4.Auth.Domain.Skills;
using Libr4.Auth.Domain.Sso;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Libr4.Auth.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfig : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> b)
    {
        b.ToTable("user_profiles");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.Headline).HasMaxLength(200);
        b.Property(x => x.Bio).HasMaxLength(4000);
        b.Property(x => x.Location).HasMaxLength(120);
        b.Property(x => x.TimeZone).HasMaxLength(64);
        b.Property(x => x.AvatarUrl).HasMaxLength(500);
        b.Property(x => x.CoverUrl).HasMaxLength(500);
        b.Property(x => x.WebsiteUrl).HasMaxLength(500);
        b.Property(x => x.HourlyRateCurrency).HasMaxLength(3);
        b.Property(x => x.Availability).HasConversion<int>();
        b.Property(x => x.HourlyRate).HasPrecision(18, 2);

        b.OwnsMany(x => x.Skills, s =>
        {
            s.ToTable("profile_skills");
            s.WithOwner().HasForeignKey(x => x.ProfileId);
            s.HasKey(x => x.Id);
            s.Property(x => x.Name).HasMaxLength(80).IsRequired();
            s.Property(x => x.Level).HasConversion<int>();
            s.HasIndex(x => new { x.ProfileId, x.Name }).IsUnique();
        });
        b.OwnsMany(x => x.Languages, l =>
        {
            l.ToTable("profile_languages");
            l.WithOwner().HasForeignKey(x => x.ProfileId);
            l.HasKey(x => x.Id);
            l.Property(x => x.Code).HasMaxLength(8).IsRequired();
            l.Property(x => x.Proficiency).HasConversion<int>();
        });
        b.OwnsMany(x => x.Socials, s =>
        {
            s.ToTable("profile_socials");
            s.WithOwner().HasForeignKey(x => x.ProfileId);
            s.HasKey(x => x.Id);
            s.Property(x => x.Platform).HasConversion<int>();
            s.Property(x => x.Url).HasMaxLength(500).IsRequired();
        });
    }
}

public sealed class KycVerificationConfig : IEntityTypeConfiguration<KycVerification>
{
    public void Configure(EntityTypeBuilder<KycVerification> b)
    {
        b.ToTable("kyc_verifications");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.Property(x => x.Level).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.RiskRating).HasConversion<int>();
        b.Property(x => x.Provider).HasMaxLength(50);
        b.Property(x => x.ExternalRefId).HasMaxLength(200);
        b.Property(x => x.FullName).HasMaxLength(200);
        b.Property(x => x.Nationality).HasMaxLength(3);
        b.Property(x => x.CountryOfResidence).HasMaxLength(3);
        b.Property(x => x.RejectionReason).HasMaxLength(1000);

        b.OwnsMany(x => x.Documents, d =>
        {
            d.ToTable("kyc_documents");
            d.WithOwner().HasForeignKey(x => x.VerificationId);
            d.HasKey(x => x.Id);
            d.Property(x => x.Type).HasConversion<int>();
            d.Property(x => x.VerificationResult).HasConversion<int?>();
            d.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
        });
        b.OwnsMany(x => x.Checks, c =>
        {
            c.ToTable("kyc_checks");
            c.WithOwner().HasForeignKey(x => x.VerificationId);
            c.HasKey(x => x.Id);
            c.Property(x => x.Type).HasConversion<int>();
            c.Property(x => x.Result).HasConversion<int>();
            c.Property(x => x.Details).HasMaxLength(2000);
        });
    }
}

public sealed class OnboardingProgressConfig : IEntityTypeConfiguration<OnboardingProgress>
{
    public void Configure(EntityTypeBuilder<OnboardingProgress> b)
    {
        b.ToTable("onboarding_progresses");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.Flow }).IsUnique();
        b.Property(x => x.Flow).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.OwnsMany(x => x.Steps, s =>
        {
            s.ToTable("onboarding_steps");
            s.WithOwner().HasForeignKey(x => x.ProgressId);
            s.HasKey(x => x.Id);
            s.Property(x => x.Key).HasMaxLength(80).IsRequired();
            s.Property(x => x.PayloadJson).HasColumnType("jsonb");
        });
    }
}

public sealed class UserLevelConfig : IEntityTypeConfiguration<UserLevel>
{
    public void Configure(EntityTypeBuilder<UserLevel> b)
    {
        b.ToTable("user_levels");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.ReputationScore).HasPrecision(5, 2);
        b.OwnsMany(x => x.Events, e =>
        {
            e.ToTable("xp_events");
            e.WithOwner().HasForeignKey(x => x.UserLevelId);
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasConversion<int>();
            e.Property(x => x.ReferenceId).HasMaxLength(80);
        });
    }
}

public sealed class ApiKeyConfig : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> b)
    {
        b.ToTable("api_keys");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.KeyHash).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.KeyHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.KeyPrefix).HasMaxLength(16).IsRequired();
        b.Property(x => x.Scopes).HasConversion<long>();
        b.Property(x => x.RevokedReason).HasMaxLength(500);
    }
}

public sealed class ExternalLoginConfig : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> b)
    {
        b.ToTable("external_logins");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
        b.HasIndex(x => x.UserId);
        b.Property(x => x.Provider).HasConversion<int>();
        b.Property(x => x.ProviderUserId).HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasMaxLength(320);
        b.Property(x => x.DisplayName).HasMaxLength(200);
        b.Property(x => x.AvatarUrl).HasMaxLength(500);
    }
}

public sealed class GdprRequestConfig : IEntityTypeConfiguration<GdprRequest>
{
    public void Configure(EntityTypeBuilder<GdprRequest> b)
    {
        b.ToTable("gdpr_requests");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.Property(x => x.ExportFileUrl).HasMaxLength(500);
    }
}

public sealed class ConsentRecordConfig : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> b)
    {
        b.ToTable("consents");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.Type, x.Version });
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Version).HasMaxLength(20).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.Property(x => x.UserAgent).HasMaxLength(500);
    }
}

public sealed class SkillTestConfig : IEntityTypeConfiguration<SkillTest>
{
    public void Configure(EntityTypeBuilder<SkillTest> b)
    {
        b.ToTable("skill_tests");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.SkillName);
        b.Property(x => x.SkillName).HasMaxLength(80).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Kind).HasConversion<int>();
    }
}

public sealed class SkillTestAttemptConfig : IEntityTypeConfiguration<SkillTestAttempt>
{
    public void Configure(EntityTypeBuilder<SkillTestAttempt> b)
    {
        b.ToTable("skill_test_attempts");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.SkillTestId });
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.AnswersJson).HasColumnType("jsonb");
    }
}

public sealed class SkillCertificateConfig : IEntityTypeConfiguration<SkillCertificate>
{
    public void Configure(EntityTypeBuilder<SkillCertificate> b)
    {
        b.ToTable("skill_certificates");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.CertificateNumber).IsUnique();
        b.Property(x => x.SkillName).HasMaxLength(80).IsRequired();
        b.Property(x => x.CertificateNumber).HasMaxLength(40).IsRequired();
    }
}

public sealed class OrganizationConfig : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> b)
    {
        b.ToTable("organizations");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.OwnerId);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.LogoUrl).HasMaxLength(500);
        b.Property(x => x.WebsiteUrl).HasMaxLength(500);
        b.Property(x => x.Plan).HasConversion<int>();

        b.OwnsMany(x => x.Members, m =>
        {
            m.ToTable("organization_members");
            m.WithOwner().HasForeignKey(x => x.OrganizationId);
            m.HasKey(x => x.Id);
            m.Property(x => x.Role).HasConversion<int>();
            m.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        });
        b.OwnsMany(x => x.Invites, i =>
        {
            i.ToTable("organization_invites");
            i.WithOwner().HasForeignKey(x => x.OrganizationId);
            i.HasKey(x => x.Id);
            i.Property(x => x.Email).HasMaxLength(320).IsRequired();
            i.Property(x => x.Role).HasConversion<int>();
            i.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            i.HasIndex(x => x.TokenHash).IsUnique();
        });
    }
}

public sealed class SkillCalibrationConfig : IEntityTypeConfiguration<SkillCalibration>
{
    public void Configure(EntityTypeBuilder<SkillCalibration> b)
    {
        b.ToTable("skill_calibrations");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.SkillTestId).IsUnique();
        b.Property(x => x.SkillName).HasMaxLength(80).IsRequired();
        b.Property(x => x.CurrentDifficulty).HasPrecision(5, 4);
        b.Property(x => x.PassRate).HasPrecision(5, 4);
        b.Property(x => x.AverageScore).HasPrecision(10, 2);
    }
}

public sealed class SecurityChallengeConfig : IEntityTypeConfiguration<SecurityChallenge>
{
    public void Configure(EntityTypeBuilder<SecurityChallenge> b)
    {
        b.ToTable("security_challenges");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.PurposeKey });
        b.Property(x => x.Kind).HasConversion<int>();
        b.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.PurposeKey).HasMaxLength(200).IsRequired();
    }
}
