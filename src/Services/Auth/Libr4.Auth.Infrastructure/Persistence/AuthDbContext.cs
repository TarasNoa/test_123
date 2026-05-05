using Libr4.Auth.Application.Abstractions;
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
using Libr4.Auth.Domain.Users;
using Libr4.Shared.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Infrastructure.Persistence;

public sealed class AuthDbContext : DbContextBase, IAuthDbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options, IPublisher publisher) : base(options, publisher) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<OnboardingProgress> OnboardingProgresses => Set<OnboardingProgress>();
    public DbSet<UserLevel> UserLevels => Set<UserLevel>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<GdprRequest> GdprRequests => Set<GdprRequest>();
    public DbSet<ConsentRecord> Consents => Set<ConsentRecord>();
    public DbSet<SkillTest> SkillTests => Set<SkillTest>();
    public DbSet<SkillTestAttempt> SkillTestAttempts => Set<SkillTestAttempt>();
    public DbSet<SkillCertificate> SkillCertificates => Set<SkillCertificate>();
    public DbSet<SkillCalibration> SkillCalibrations => Set<SkillCalibration>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<SecurityChallenge> SecurityChallenges => Set<SecurityChallenge>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
        base.OnModelCreating(b);
    }
}
