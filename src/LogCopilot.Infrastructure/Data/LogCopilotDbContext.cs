using Microsoft.EntityFrameworkCore;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Data.Configurations;

namespace LogCopilot.Infrastructure.Data;

public class LogCopilotDbContext : DbContext
{
    public LogCopilotDbContext(DbContextOptions<LogCopilotDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UploadSession> UploadSessions { get; set; }
    public DbSet<UploadedFile> UploadedFiles { get; set; }
    public DbSet<IngestionJob> IngestionJobs { get; set; }
    public DbSet<LogSource> LogSources { get; set; }
    public DbSet<LogEvent> LogEvents { get; set; }
    public DbSet<HttpEventDetails> HttpEventDetails { get; set; }
    public DbSet<ExceptionDetails> ExceptionDetails { get; set; }
    public DbSet<RequestTrace> RequestTraces { get; set; }
    public DbSet<TraceEvent> TraceEvents { get; set; }
    public DbSet<IssueCluster> IssueClusters { get; set; }
    public DbSet<ClusterEvent> ClusterEvents { get; set; }
    public DbSet<IncidentReport> IncidentReports { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<BillingPlan> BillingPlans { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationMemberConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new UploadSessionConfiguration());
        modelBuilder.ApplyConfiguration(new UploadedFileConfiguration());
        modelBuilder.ApplyConfiguration(new IngestionJobConfiguration());
        modelBuilder.ApplyConfiguration(new LogSourceConfiguration());
        modelBuilder.ApplyConfiguration(new LogEventConfiguration());
        modelBuilder.ApplyConfiguration(new HttpEventDetailsConfiguration());
        modelBuilder.ApplyConfiguration(new ExceptionDetailsConfiguration());
        modelBuilder.ApplyConfiguration(new RequestTraceConfiguration());
        modelBuilder.ApplyConfiguration(new TraceEventConfiguration());
        modelBuilder.ApplyConfiguration(new IssueClusterConfiguration());
        modelBuilder.ApplyConfiguration(new ClusterEventConfiguration());
        modelBuilder.ApplyConfiguration(new IncidentReportConfiguration());
        modelBuilder.ApplyConfiguration(new ApiKeyConfiguration());
        modelBuilder.ApplyConfiguration(new BillingPlanConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
    }
}
