using Libr4.Tasks.Application.Abstractions;
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
using Libr4.Tasks.Domain.Posts;
using Libr4.Tasks.Domain.TimeTracking;
using Libr4.Tasks.Domain.WorkDelivery;
using Libr4.Shared.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Infrastructure.Persistence;

public sealed class TasksDbContext : DbContextBase, ITasksDbContext
{
    public TasksDbContext(DbContextOptions<TasksDbContext> options, IPublisher publisher) : base(options, publisher) { }

    public DbSet<TaskAggregate> Tasks => Set<TaskAggregate>();
    public DbSet<Libr4.Tasks.Domain.Tasks.Application> Applications => Set<Libr4.Tasks.Domain.Tasks.Application>();
    public DbSet<Libr4.Tasks.Domain.Reviews.Review> Reviews => Set<Libr4.Tasks.Domain.Reviews.Review>();
    public DbSet<Libr4.Tasks.Domain.Portfolio.PortfolioItem> PortfolioItems => Set<Libr4.Tasks.Domain.Portfolio.PortfolioItem>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<View> Views => Set<View>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<CertificateVerification> CertificateVerifications => Set<CertificateVerification>();
    public DbSet<CertificateEndorsement> CertificateEndorsements => Set<CertificateEndorsement>();
    public DbSet<CertificateAttachment> CertificateAttachments => Set<CertificateAttachment>();
    public DbSet<CRMAccount> CRMAccounts => Set<CRMAccount>();
    public DbSet<CRMContact> CRMContacts => Set<CRMContact>();
    public DbSet<CRMDeal> CRMDeals => Set<CRMDeal>();
    public DbSet<CRMTask> CRMTasks => Set<CRMTask>();
    public DbSet<CRMActivity> CRMActivities => Set<CRMActivity>();
    public DbSet<CRMPipeline> CRMPipelines => Set<CRMPipeline>();
    public DbSet<BlindApplication> BlindApplications => Set<BlindApplication>();
    public DbSet<Domain.WorkDelivery.WorkDelivery> WorkDeliveries => Set<Domain.WorkDelivery.WorkDelivery>();
    public DbSet<WorkDeliveryFile> WorkDeliveryFiles => Set<WorkDeliveryFile>();
    public DbSet<PreviewSession> PreviewSessions => Set<PreviewSession>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<DisputeMessage> DisputeMessages => Set<DisputeMessage>();
    public DbSet<DisputeEvidence> DisputeEvidences => Set<DisputeEvidence>();
    public DbSet<DisputeResolution> DisputeResolutions => Set<DisputeResolution>();
    public DbSet<DisputeArbitrator> DisputeArbitrators => Set<DisputeArbitrator>();
    public DbSet<TimeSession> TimeSessions => Set<TimeSession>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Screenshot> Screenshots => Set<Screenshot>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<AntiCheatAlert> AntiCheatAlerts => Set<AntiCheatAlert>();
    public DbSet<TimeReport> TimeReports => Set<TimeReport>();
    public DbSet<TimeTrackingSettings> TimeTrackingSettings => Set<TimeTrackingSettings>();
    public DbSet<FreelancerTeam> FreelancerTeams => Set<FreelancerTeam>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Libr4.Tasks.Domain.TeamsPortfolio.Review> TeamReviews => Set<Libr4.Tasks.Domain.TeamsPortfolio.Review>();
    public DbSet<RateHistory> RateHistories => Set<RateHistory>();
    public DbSet<SkillTest> SkillTests => Set<SkillTest>();
    public DbSet<SkillTestResult> SkillTestResults => Set<SkillTestResult>();
    public DbSet<ClientVerification> ClientVerifications => Set<ClientVerification>();
    public DbSet<PortfolioAnalytics> PortfolioAnalytics => Set<PortfolioAnalytics>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<PostComment> PostComments => Set<PostComment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(TasksDbContext).Assembly);
        base.OnModelCreating(b);
    }
}
