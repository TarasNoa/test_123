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
using Microsoft.EntityFrameworkCore;

namespace Libr4.Auth.Application.Abstractions;

public interface IAuthDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserToken> UserTokens { get; }
    DbSet<UserProfile> Profiles { get; }
    DbSet<KycVerification> KycVerifications { get; }
    DbSet<OnboardingProgress> OnboardingProgresses { get; }
    DbSet<UserLevel> UserLevels { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<ExternalLogin> ExternalLogins { get; }
    DbSet<GdprRequest> GdprRequests { get; }
    DbSet<ConsentRecord> Consents { get; }
    DbSet<SkillTest> SkillTests { get; }
    DbSet<SkillTestAttempt> SkillTestAttempts { get; }
    DbSet<SkillCertificate> SkillCertificates { get; }
    DbSet<SkillCalibration> SkillCalibrations { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<SecurityChallenge> SecurityChallenges { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
