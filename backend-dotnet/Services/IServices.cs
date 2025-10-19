using CodePulseApi.DTOs;

namespace CodePulseApi.Services;

public interface IAzureAIFoundryService
{
    Task<CodeReviewResultDto> ReviewCodeAsync(string code, string fileName, string? pullRequestContext = null);
    Task<string> SummarizePullRequestAsync(PullRequestSummaryDto pullRequest);
}

public interface IWebhookService
{
    Task<bool> ProcessWebhookAsync(object webhook, string provider, string? signature = null);
    Task<bool> ProcessWebhookAsync(AzureDevOpsWebhookDto webhook);
}


public interface IRepositoryService
{
    Task<List<RepositoryResponseDto>> GetRepositoriesAsync();
    Task<RepositoryResponseDto?> GetRepositoryByIdAsync(string id);
    Task<AnalyticsDataDto> GetRepositoryAnalyticsAsync(string id, string period = "30d");
}

public interface ILeaderboardService
{
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string period = "MONTHLY", int limit = 10, string metric = "TotalCommits");
    Task<EngineerDetailsDto?> GetEngineerDetailsAsync(string id, string period = "MONTHLY");
    Task<List<LeaderboardTrendDto>> GetLeaderboardTrendsAsync(string period = "MONTHLY", string? engineerId = null);
}

public interface IReviewService
{
    Task<PaginatedResponseDto<ReviewResponseDto>> GetReviewsAsync(int page = 1, int limit = 20, string? type = null, string? status = null);
    Task<ReviewResponseDto?> GetReviewByIdAsync(string id);
    Task<ReviewResponseDto> CreateReviewAsync(CreateReviewRequestDto request);
    Task<ReviewResponseDto?> UpdateReviewAsync(string id, UpdateReviewRequestDto request);
    Task<ReviewResponseDto> TriggerAIReviewAsync(string pullRequestId);
    Task<ReviewStatsDto> GetReviewStatsAsync(string period = "30d");
}

public interface IAnalyticsService
{
    Task<AnalyticsDataDto> GetDashboardAnalyticsAsync(string period = "30d");
    Task<EngineerAnalyticsDto> GetEngineerAnalyticsAsync(string id, string period = "30d");
    Task<RepositoryAnalyticsDto> GetRepositoryAnalyticsAsync(string id, string period = "30d");
    Task<TeamComparisonDto> GetTeamComparisonAsync(string period = "30d");
}

// Additional DTOs for services
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

public class EngineerDetailsDto
{
    public EngineerResponseDto Engineer { get; set; } = new();
    public List<LeaderboardStatsDto> HistoricalStats { get; set; } = new();
    public List<PullRequestResponseDto> RecentPullRequests { get; set; } = new();
    public List<CommitResponseDto> RecentCommits { get; set; } = new();
    public EngineerMetricsDto Metrics { get; set; } = new();
}

public class EngineerMetricsDto
{
    public int TotalReviewsGiven { get; set; }
    public double AverageScoreReceived { get; set; }
}

public class LeaderboardTrendDto
{
    public string Period { get; set; } = string.Empty;
    public List<EngineerTrendDto> Engineers { get; set; } = new();
    public TotalStatsDto Totals { get; set; } = new();
}

public class EngineerTrendDto
{
    public EngineerResponseDto Engineer { get; set; } = new();
    public LeaderboardStatsDto Stats { get; set; } = new();
}

public class TotalStatsDto
{
    public int Commits { get; set; }
    public int PullRequests { get; set; }
    public double AverageQuality { get; set; }
}

public class ReviewStatsDto
{
    public string Period { get; set; } = string.Empty;
    public int TotalReviews { get; set; }
    public int AiReviews { get; set; }
    public int HumanReviews { get; set; }
    public double AverageScore { get; set; }
    public List<StatusDistributionDto> StatusDistribution { get; set; } = new();
}

public class StatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class EngineerAnalyticsDto
{
    public EngineerResponseDto Engineer { get; set; } = new();
    public string Period { get; set; } = string.Empty;
    public EngineerMetricsDetailDto Metrics { get; set; } = new();
}

public class EngineerMetricsDetailDto
{
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public double AverageReviewScore { get; set; }
    public Dictionary<string, int> CommitsByRepository { get; set; } = new();
    public List<TrendDataDto> DailyCommits { get; set; } = new();
}

public class RepositoryAnalyticsDto
{
    public RepositoryResponseDto Repository { get; set; } = new();
    public string Period { get; set; } = string.Empty;
    public RepositoryMetricsDto Metrics { get; set; } = new();
}

public class RepositoryMetricsDto
{
    public int CommitCount { get; set; }
    public int PullRequestCount { get; set; }
    public int ActiveContributors { get; set; }
    public double AverageReviewScore { get; set; }
    public List<TrendDataDto> CommitTrends { get; set; } = new();
}

public class TeamComparisonDto
{
    public string Period { get; set; } = string.Empty;
    public List<EngineerComparisonDto> Engineers { get; set; } = new();
}

public class EngineerComparisonDto
{
    public EngineerResponseDto Engineer { get; set; } = new();
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public double AverageReviewScore { get; set; }
}
