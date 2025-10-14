using CodePulseApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CodePulseApi.Data;

public class CodePulseDbContext : DbContext
{
    public CodePulseDbContext(DbContextOptions<CodePulseDbContext> options) : base(options)
    {
    }

    public DbSet<Repository> Repositories { get; set; }
    public DbSet<Engineer> Engineers { get; set; }
    public DbSet<PullRequest> PullRequests { get; set; }
    public DbSet<Commit> Commits { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<LeaderboardStats> LeaderboardStats { get; set; }
    public DbSet<WebhookEvent> WebhookEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Repository unique constraint
        modelBuilder.Entity<Repository>()
            .HasIndex(r => new { r.ProjectId, r.Name })
            .IsUnique();

        // Engineer unique constraint
        modelBuilder.Entity<Engineer>()
            .HasIndex(e => e.Email)
            .IsUnique();

        // PullRequest unique constraint
        modelBuilder.Entity<PullRequest>()
            .HasIndex(pr => new { pr.RepositoryId, pr.PrId })
            .IsUnique();

        // Commit unique constraint
        modelBuilder.Entity<Commit>()
            .HasIndex(c => c.CommitId)
            .IsUnique();

        // LeaderboardStats unique constraint
        modelBuilder.Entity<LeaderboardStats>()
            .HasIndex(ls => new { ls.EngineerId, ls.Period, ls.PeriodStart })
            .IsUnique();

        // Configure relationships
        modelBuilder.Entity<PullRequest>()
            .HasOne(pr => pr.Repository)
            .WithMany(r => r.PullRequests)
            .HasForeignKey(pr => pr.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PullRequest>()
            .HasOne(pr => pr.CreatedBy)
            .WithMany(e => e.PullRequests)
            .HasForeignKey(pr => pr.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Commit>()
            .HasOne(c => c.Repository)
            .WithMany(r => r.Commits)
            .HasForeignKey(c => c.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Commit>()
            .HasOne(c => c.PullRequest)
            .WithMany(pr => pr.Commits)
            .HasForeignKey(c => c.PullRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Commit>()
            .HasOne(c => c.AuthorEng)
            .WithMany(e => e.Commits)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.PullRequest)
            .WithMany(pr => pr.Reviews)
            .HasForeignKey(r => r.PullRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Commit)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CommitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Reviewer)
            .WithMany(e => e.Reviews)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LeaderboardStats>()
            .HasOne(ls => ls.Engineer)
            .WithMany(e => e.LeaderboardStats)
            .HasForeignKey(ls => ls.EngineerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure decimal precision
        modelBuilder.Entity<Review>()
            .Property(r => r.Score)
            .HasPrecision(5, 2);

        modelBuilder.Entity<LeaderboardStats>()
            .Property(ls => ls.AverageReviewScore)
            .HasPrecision(5, 2);

        modelBuilder.Entity<LeaderboardStats>()
            .Property(ls => ls.CodeQualityScore)
            .HasPrecision(5, 2);

        // Configure text fields
        modelBuilder.Entity<PullRequest>()
            .Property(pr => pr.Description)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Commit>()
            .Property(c => c.Message)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Review>()
            .Property(r => r.Content)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Review>()
            .Property(r => r.Suggestions)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<WebhookEvent>()
            .Property(we => we.Payload)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<WebhookEvent>()
            .Property(we => we.Error)
            .HasColumnType("nvarchar(max)");
    }
}
