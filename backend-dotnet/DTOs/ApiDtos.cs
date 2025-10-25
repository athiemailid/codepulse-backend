using System.ComponentModel.DataAnnotations;

namespace CodePulseApi.DTOs;

// Azure DevOps Webhook DTOs
public class AzureDevOpsWebhookDto
{
    public string SubscriptionId { get; set; } = string.Empty;
    public int NotificationId { get; set; }
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PublisherId { get; set; } = string.Empty;
    public MessageDto Message { get; set; } = new();
    public MessageDto DetailedMessage { get; set; } = new();
    public object Resource { get; set; } = new();
    public string ResourceVersion { get; set; } = string.Empty;
    public ResourceContainersDto ResourceContainers { get; set; } = new();
    public DateTime CreatedDate { get; set; }
}

public class MessageDto
{
    public string Text { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
}

public class ResourceContainersDto
{
    public ContainerDto Collection { get; set; } = new();
    public ContainerDto Account { get; set; } = new();
    public ContainerDto Project { get; set; } = new();
}

public class ContainerDto
{
    public string Id { get; set; } = string.Empty;
}

public class PullRequestResourceDto
{
    public RepositoryDto Repository { get; set; } = new();
    public int PullRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public UserDto CreatedBy { get; set; } = new();
    public DateTime CreationDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceRefName { get; set; } = string.Empty;
    public string TargetRefName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public CommitDto? LastMergeSourceCommit { get; set; }
    public CommitDto? LastMergeTargetCommit { get; set; }
}

public class GitPushResourceDto
{
    public List<CommitDto> Commits { get; set; } = new();
    public List<RefUpdateDto> RefUpdates { get; set; } = new();
    public RepositoryDto Repository { get; set; } = new();
    public UserDto PushedBy { get; set; } = new();
    public int PushId { get; set; }
    public DateTime Date { get; set; }
    public string Url { get; set; } = string.Empty;
}


public class ProjectDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UserDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string UniqueName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}



public class RefUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string OldObjectId { get; set; } = string.Empty;
    public string NewObjectId { get; set; } = string.Empty;
}

// Response DTOs
public class RepositoryResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalCommits { get; set; }
    public List<PullRequestResponseDto> RecentPullRequests { get; set; } = new();
    public List<CommitResponseDto> RecentCommits { get; set; } = new();
}

public class EngineerResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PullRequestResponseDto
{
    public string Id { get; set; } = string.Empty;
    public int PrId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SourceRefName { get; set; } = string.Empty;
    public string TargetRefName { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string RepositoryId { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public EngineerResponseDto CreatedBy { get; set; } = new();
    public List<ReviewResponseDto> Reviews { get; set; } = new();
    public int TotalCommits { get; set; }
}

public class CommitResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string CommitId { get; set; } = string.Empty;
    public string CommitHash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public DateTime CommitDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Url { get; set; } = string.Empty;
    public int ChangedFiles { get; set; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public string RepositoryId { get; set; } = string.Empty;
    public string? PullRequestId { get; set; }
    public EngineerResponseDto? AuthorEng { get; set; }
    public List<ReviewResponseDto> Reviews { get; set; } = new();
}

public class ReviewResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double? Score { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PullRequestId { get; set; }
    public string? CommitId { get; set; }
    public EngineerResponseDto? Reviewer { get; set; }
}

public class LeaderboardEntryDto
{
    public EngineerResponseDto Engineer { get; set; } = new();
    public LeaderboardStatsDto Stats { get; set; } = new();
    public int Rank { get; set; }
    public string Period { get; set; } = string.Empty;
}

public class LeaderboardStatsDto
{
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalLinesAdded { get; set; }
    public int TotalLinesDeleted { get; set; }
    public int TotalFilesChanged { get; set; }
    public double AverageReviewScore { get; set; }
    public int TotalReviewsReceived { get; set; }
    public int TotalReviewsGiven { get; set; }
    public int TotalReviews { get; set; }
    public double CodeQualityScore { get; set; }
    public string Period { get; set; } = string.Empty;
}

public class AnalyticsDataDto
{
    public string Period { get; set; } = string.Empty;
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalEngineers { get; set; }
    public int TotalRepositories { get; set; }
    public int TotalReviews { get; set; }
    public double AverageReviewScore { get; set; }
    public List<LanguageStatsDto> TopLanguages { get; set; } = new();
    public List<TrendDataDto> CommitTrends { get; set; } = new();
    public List<TrendDataDto> QualityTrends { get; set; } = new();
    public List<TrendDataDto> TrendData { get; set; } = new();
    public List<EngineerResponseDto> TopPerformers { get; set; } = new();
    public List<RepositoryStatsDto> RepositoryStats { get; set; } = new();
}

public class LanguageStatsDto
{
    public string Language { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TrendDataDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public int Value { get; set; }
    public double AverageScore { get; set; }
}

// Request DTOs
public class CreateReviewRequestDto
{
    public string? PullRequestId { get; set; }
    public string? CommitId { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [Range(0, 10)]
    public double? Score { get; set; }
    
    [Required]
    public string Status { get; set; } = string.Empty;
    
    public string? ReviewerId { get; set; }
}

public class UpdateReviewRequestDto
{
    public string? Content { get; set; }
    
    [Range(0, 10)]
    public double? Score { get; set; }
    
    public string? Status { get; set; }
}

public class CodeReviewResultDto
{
    public double Score { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
    public List<CodeIssueDto> Issues { get; set; } = new();
}

public class CodeIssueDto
{
    public int Line { get; set; }
    public string Severity { get; set; } = string.Empty; // low, medium, high
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
}

public class PaginatedResponseDto<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
}

public class PaginationDto
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public int Pages { get; set; }
}

// Additional DTOs for Services
public class CreateRepositoryDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string ProjectId { get; set; } = "default";
    
    public string? Description { get; set; }
}

public class UpdateRepositoryDto
{
    [MaxLength(200)]
    public string? Name { get; set; }
    
    [MaxLength(500)]
    public string? Url { get; set; }
    
    public string? Description { get; set; }
    
    public bool? IsActive { get; set; }
}

public class RepositoryStatsDto
{
    public string RepositoryId { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public RepositoryResponseDto Repository { get; set; } = new();
    public int TotalPullRequests { get; set; }
    public int TotalCommits { get; set; }
    public int CommitCount { get; set; }
    public int PullRequestCount { get; set; }
    public int ActiveEngineers { get; set; }
    public double AverageReviewScore { get; set; }
    public DateTime? LastActivityDate { get; set; }
}

public class EngineerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
}

public class PullRequestDto
{
    public string Id { get; set; } = string.Empty;
    public int PrId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SourceRefName { get; set; } = string.Empty;
    public string TargetRefName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string RepositoryId { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public int TotalReviews { get; set; }
    public double AverageReviewScore { get; set; }
}

public class ReviewDto
{
    public string Id { get; set; } = string.Empty;
    public string PullRequestId { get; set; } = string.Empty;
    public string PullRequestTitle { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public string EngineerName { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public string ReviewerEmail { get; set; } = string.Empty;
    public string ReviewType { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public string? Suggestions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateReviewDto
{
    [Required]
    public string PullRequestId { get; set; } = string.Empty;
    
    [Required]
    public string ReviewerName { get; set; } = string.Empty;
    
    [Required]
    public string ReviewerEmail { get; set; } = string.Empty;
    
    public string ReviewType { get; set; } = "Manual";
    
    [Range(0, 10)]
    public double Score { get; set; }
    
    [Required]
    public string Feedback { get; set; } = string.Empty;
    
    public string? Suggestions { get; set; }
}

public class UpdateReviewDto
{
    [Range(0, 10)]
    public double? Score { get; set; }
    
    public string? Feedback { get; set; }
    
    public string? Suggestions { get; set; }
}

public class ReviewSummaryDto
{
    public string RepositoryId { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public int TotalReviews { get; set; }
    public double AverageScore { get; set; }
    public double HighestScore { get; set; }
    public double LowestScore { get; set; }
    public int AIReviews { get; set; }
    public int HumanReviews { get; set; }
    public DateTime? LastReviewDate { get; set; }
}

public class EngineerStatsDto
{
    public string EngineerId { get; set; } = string.Empty;
    public string EngineerName { get; set; } = string.Empty;
    public string EngineerEmail { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalLinesAdded { get; set; }
    public int TotalLinesDeleted { get; set; }
    public int TotalFilesChanged { get; set; }
    public double AverageReviewScore { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int CommitsThisWeek { get; set; }
    public int CommitsThisMonth { get; set; }
    public int PullRequestsThisWeek { get; set; }
    public int PullRequestsThisMonth { get; set; }
    public double Score { get; set; }
}

public class TopPerformerDto
{
    public string EngineerId { get; set; } = string.Empty;
    public string EngineerName { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public int TotalCommits { get; set; }
    public double AverageReviewScore { get; set; }
    public int TotalContributions { get; set; }
    public double Score { get; set; }
}

public class DashboardMetricsDto
{
    public int TotalRepositories { get; set; }
    public int TotalEngineers { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalCommits { get; set; }
    public int TotalReviews { get; set; }
    public int PullRequestsThisWeek { get; set; }
    public int CommitsThisWeek { get; set; }
    public int ReviewsThisWeek { get; set; }
    public int PullRequestsThisMonth { get; set; }
    public int CommitsThisMonth { get; set; }
    public int ReviewsThisMonth { get; set; }
    public double AverageReviewScore { get; set; }
    public double AverageReviewScoreThisWeek { get; set; }
    public double AverageReviewScoreThisMonth { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ActivityTrendDto
{
    public DateTime Date { get; set; }
    public int Commits { get; set; }
    public int PullRequests { get; set; }
    public int Reviews { get; set; }
    public double AverageReviewScore { get; set; }
}

public class RepositoryAnalyticsDto
{
    public string RepositoryId { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalReviews { get; set; }
    public int TotalEngineers { get; set; }
    public double AverageReviewScore { get; set; }
    public int TotalLinesChanged { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int CommitsThisWeek { get; set; }
    public int PullRequestsThisWeek { get; set; }
    public int ReviewsThisWeek { get; set; }
}

public class EngineerPerformanceDto
{
    public string EngineerId { get; set; } = string.Empty;
    public string EngineerName { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalReviews { get; set; }
    public double AverageReviewScore { get; set; }
    public int TotalLinesAdded { get; set; }
    public int TotalLinesDeleted { get; set; }
    public double ProductivityScore { get; set; }
    public double QualityScore { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int CommitsThisWeek { get; set; }
    public int PullRequestsThisWeek { get; set; }
}

public class CodeQualityMetricsDto
{
    public int TotalReviews { get; set; }
    public double AverageScore { get; set; }
    public double HighestScore { get; set; }
    public double LowestScore { get; set; }
    public Dictionary<string, int> ScoreDistribution { get; set; } = new();
    public int AIReviews { get; set; }
    public int HumanReviews { get; set; }
    public string? RepositoryId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class MonthlyStatsDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int Commits { get; set; }
    public int PullRequests { get; set; }
    public int Reviews { get; set; }
    public double AverageReviewScore { get; set; }
    public int TotalLinesChanged { get; set; }
    public int ActiveEngineers { get; set; }
}

public class PullRequestSummaryDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CommitSummaryDto> Commits { get; set; } = new();
}

public class CommitSummaryDto
{
    public string Message { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int ChangedFiles { get; set; }
}

public class WebhookEventDto
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public bool IsProcessed { get; set; }
    public string? ProcessingResult { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
