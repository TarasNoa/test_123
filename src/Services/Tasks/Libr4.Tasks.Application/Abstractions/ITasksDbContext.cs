using Libr4.Tasks.Domain.BlindApplications;
using Libr4.Tasks.Domain.Certificates;
using Libr4.Tasks.Domain.CRM;
using Libr4.Tasks.Domain.DisputeResolution;
using Libr4.Tasks.Domain.Interactions;
using Libr4.Tasks.Domain.Portfolio;
using Libr4.Tasks.Domain.Projects;
using Libr4.Tasks.Domain.Reviews;
using Libr4.Tasks.Domain.Tasks;
using Libr4.Tasks.Domain.TeamsPortfolio;
using Libr4.Tasks.Domain.TimeTracking;
using Libr4.Tasks.Domain.Posts;
using Libr4.Tasks.Domain.WorkDelivery;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Abstractions;

public interface ITasksDbContext
{
    DbSet<TaskAggregate> Tasks { get; }
    DbSet<Libr4.Tasks.Domain.Tasks.Application> Applications { get; }
    DbSet<Libr4.Tasks.Domain.Reviews.Review> Reviews { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<ProjectTask> ProjectTasks { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<Like> Likes { get; }
    DbSet<Bookmark> Bookmarks { get; }
    DbSet<Follow> Follows { get; }
    DbSet<View> Views { get; }
    DbSet<Libr4.Tasks.Domain.Portfolio.PortfolioItem> PortfolioItems { get; }
    DbSet<Certificate> Certificates { get; }
    DbSet<CertificateVerification> CertificateVerifications { get; }
    DbSet<CertificateEndorsement> CertificateEndorsements { get; }
    DbSet<CertificateAttachment> CertificateAttachments { get; }
    DbSet<CRMAccount> CRMAccounts { get; }
    DbSet<CRMContact> CRMContacts { get; }
    DbSet<CRMDeal> CRMDeals { get; }
    DbSet<CRMTask> CRMTasks { get; }
    DbSet<CRMActivity> CRMActivities { get; }
    DbSet<CRMPipeline> CRMPipelines { get; }
    DbSet<BlindApplication> BlindApplications { get; }
    DbSet<Domain.WorkDelivery.WorkDelivery> WorkDeliveries { get; }
    DbSet<WorkDeliveryFile> WorkDeliveryFiles { get; }
    DbSet<PreviewSession> PreviewSessions { get; }
    DbSet<Dispute> Disputes { get; }
    DbSet<DisputeMessage> DisputeMessages { get; }
    DbSet<DisputeEvidence> DisputeEvidences { get; }
    DbSet<DisputeResolution> DisputeResolutions { get; }
    DbSet<DisputeArbitrator> DisputeArbitrators { get; }
    DbSet<TimeSession> TimeSessions { get; }
    DbSet<TimeEntry> TimeEntries { get; }
    DbSet<Screenshot> Screenshots { get; }
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<AntiCheatAlert> AntiCheatAlerts { get; }
    DbSet<TimeReport> TimeReports { get; }
    DbSet<TimeTrackingSettings> TimeTrackingSettings { get; }
    DbSet<FreelancerTeam> FreelancerTeams { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<Libr4.Tasks.Domain.TeamsPortfolio.Review> TeamReviews { get; }
    DbSet<RateHistory> RateHistories { get; }
    DbSet<SkillTest> SkillTests { get; }
    DbSet<SkillTestResult> SkillTestResults { get; }
    DbSet<ClientVerification> ClientVerifications { get; }
    DbSet<PortfolioAnalytics> PortfolioAnalytics { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostLike> PostLikes { get; }
    DbSet<PostComment> PostComments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
