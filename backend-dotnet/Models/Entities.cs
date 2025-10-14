using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodePulseApi.Models;

public class Repository
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ProjectId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string DefaultBranch { get; set; } = "main";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<PullRequest> PullRequests { get; set; } = new List<PullRequest>();
    public virtual ICollection<Commit> Commits { get; set; } = new List<Commit>();
}

public class Engineer
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<PullRequest> PullRequests { get; set; } = new List<PullRequest>();
    public virtual ICollection<Commit> Commits { get; set; } = new List<Commit>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<LeaderboardStats> LeaderboardStats { get; set; } = new List<LeaderboardStats>();
}

public class PullRequest
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public int PrId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string SourceRefName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string TargetRefName { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // Active, Completed, Abandoned
    
    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime? ClosedDate { get; set; }
    
    // Additional properties for compatibility
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public string AuthorId { get; set; } = string.Empty;
    
    [Required]
    public string RepositoryId { get; set; } = string.Empty;
    
    [Required]
    public string CreatedById { get; set; } = string.Empty;
    
    // Navigation properties
    [ForeignKey("RepositoryId")]
    public virtual Repository Repository { get; set; } = null!;
    
    [ForeignKey("CreatedById")]
    public virtual Engineer CreatedBy { get; set; } = null!;
    
    public virtual ICollection<Commit> Commits { get; set; } = new List<Commit>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class Commit
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(50)]
    public string CommitId { get; set; } = string.Empty;

    public string CommitHash { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string AuthorEmail { get; set; } = string.Empty;
    
    public DateTime CommitDate { get; set; }
    
    // Additional properties for compatibility
    public DateTime CreatedAt { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;
    
    public int ChangedFiles { get; set; } = 0;
    
    public int Additions { get; set; } = 0;
    
    public int Deletions { get; set; } = 0;
    
    [Required]
    public string RepositoryId { get; set; } = string.Empty;
    
    public string? PullRequestId { get; set; }
    
    public string? AuthorId { get; set; }
    
    // Navigation properties
    [ForeignKey("RepositoryId")]
    public virtual Repository Repository { get; set; } = null!;
    
    [ForeignKey("PullRequestId")]
    public virtual PullRequest? PullRequest { get; set; }
    
    [ForeignKey("AuthorId")]
    public virtual Engineer? AuthorEng { get; set; }
    
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class Review
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty; // AI, HUMAN
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public double? Score { get; set; } // AI confidence score or human rating
    
    public string? Suggestions { get; set; } // JSON array of suggestions
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // PENDING, APPROVED, REJECTED, NEEDS_WORK
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public string? PullRequestId { get; set; }
    
    public string? CommitId { get; set; }
    
    public string? ReviewerId { get; set; }
    
    // Navigation properties
    [ForeignKey("PullRequestId")]
    public virtual PullRequest? PullRequest { get; set; }
    
    [ForeignKey("CommitId")]
    public virtual Commit? Commit { get; set; }
    
    [ForeignKey("ReviewerId")]
    public virtual Engineer? Reviewer { get; set; }
}

public class LeaderboardStats
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string EngineerId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Period { get; set; } = string.Empty; // WEEKLY, MONTHLY, QUARTERLY, YEARLY
    
    public DateTime PeriodStart { get; set; }
    
    public DateTime PeriodEnd { get; set; }
    
    public int TotalCommits { get; set; } = 0;
    
    public int TotalPullRequests { get; set; } = 0;
    
    public int TotalLinesAdded { get; set; } = 0;
    
    public int TotalLinesDeleted { get; set; } = 0;
    
    public int TotalFilesChanged { get; set; } = 0;
    
    public double? AverageReviewScore { get; set; }
    
    public int TotalReviewsReceived { get; set; } = 0;
    
    public int TotalReviewsGiven { get; set; } = 0;
    
    public int BugFixCommits { get; set; } = 0;
    
    public int FeatureCommits { get; set; } = 0;
    
    public double? CodeQualityScore { get; set; }
    
    // Navigation properties
    [ForeignKey("EngineerId")]
    public virtual Engineer Engineer { get; set; } = null!;
}

public class WebhookEvent
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public string Payload { get; set; } = string.Empty; // JSON payload
    
    public bool Processed { get; set; } = false;
    
    public string? Error { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
}
