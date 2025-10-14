using CodePulseApi.Data;
using CodePulseApi.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CodePulseApi.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly CodePulseDbContext _context;
    private readonly ILogger<LeaderboardService> _logger;

    public LeaderboardService(CodePulseDbContext context, ILogger<LeaderboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string period = "MONTHLY", int limit = 10, string metric = "TotalCommits")
    {
        try
        {
            var (startDate, endDate) = ParsePeriod(period);

            var engineers = await _context.Engineers
                .Where(e => e.IsActive)
                .ToListAsync();

            var leaderboardEntries = new List<LeaderboardEntryDto>();

            foreach (var engineer in engineers)
            {
                var commits = await _context.Commits
                    .CountAsync(c => c.AuthorId == engineer.Id && c.CreatedAt >= startDate && c.CreatedAt <= endDate);

                var pullRequests = await _context.PullRequests
                    .CountAsync(pr => pr.AuthorId == engineer.Id && pr.CreatedAt >= startDate && pr.CreatedAt <= endDate);

                var reviews = await _context.Reviews
                    .Where(r => r.PullRequest.AuthorId == engineer.Id && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                    .ToListAsync();

                var averageScore = reviews.Where(r => r.Score.HasValue).Any()
                    ? reviews.Where(r => r.Score.HasValue).Average(r => r.Score!.Value)
                    : 0;

                var stats = new LeaderboardStatsDto
                {
                    TotalCommits = commits,
                    TotalPullRequests = pullRequests,
                    TotalReviews = reviews.Count,
                    AverageReviewScore = averageScore,
                    Period = period
                };

                leaderboardEntries.Add(new LeaderboardEntryDto
                {
                    Engineer = new EngineerResponseDto
                    {
                        Id = engineer.Id,
                        Name = engineer.Name,
                        Email = engineer.Email,
                        AvatarUrl = engineer.AvatarUrl,
                        JoinedAt = engineer.JoinedAt,
                        IsActive = engineer.IsActive
                    },
                    Stats = stats,
                    Rank = 0 // Will be set after sorting
                });
            }

            // Sort based on metric
            var sortedEntries = metric.ToLower() switch
            {
                "totalcommits" => leaderboardEntries.OrderByDescending(e => e.Stats.TotalCommits).ToList(),
                "totalpullrequests" => leaderboardEntries.OrderByDescending(e => e.Stats.TotalPullRequests).ToList(),
                "averagereviewscore" => leaderboardEntries.OrderByDescending(e => e.Stats.AverageReviewScore).ToList(),
                _ => leaderboardEntries.OrderByDescending(e => e.Stats.TotalCommits).ToList()
            };

            // Set ranks
            for (int i = 0; i < sortedEntries.Count; i++)
            {
                sortedEntries[i].Rank = i + 1;
            }

            return sortedEntries.Take(limit).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard for period {Period}, metric {Metric}", period, metric);
            throw;
        }
    }

    public async Task<EngineerDetailsDto?> GetEngineerDetailsAsync(string id, string period = "MONTHLY")
    {
        try
        {
            var engineer = await _context.Engineers
                .FirstOrDefaultAsync(e => e.Id == id);

            if (engineer == null)
                return null;

            var (startDate, endDate) = ParsePeriod(period);

            var commits = await _context.Commits
                .Where(c => c.AuthorId == id && c.CreatedAt >= startDate && c.CreatedAt <= endDate)
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .ToListAsync();

            var pullRequests = await _context.PullRequests
                .Where(pr => pr.AuthorId == id && pr.CreatedAt >= startDate && pr.CreatedAt <= endDate)
                .OrderByDescending(pr => pr.CreatedAt)
                .Take(10)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Where(r => r.PullRequest.AuthorId == id && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .ToListAsync();

            var reviewsGiven = await _context.Reviews
                .Where(r => r.ReviewerId == id && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .CountAsync();

            var details = new EngineerDetailsDto
            {
                Engineer = new EngineerResponseDto
                {
                    Id = engineer.Id,
                    Name = engineer.Name,
                    Email = engineer.Email,
                    AvatarUrl = engineer.AvatarUrl,
                    JoinedAt = engineer.JoinedAt,
                    IsActive = engineer.IsActive
                },
                HistoricalStats = await GetHistoricalStatsAsync(id, period),
                RecentPullRequests = pullRequests.Select(pr => new PullRequestResponseDto
                {
                    Id = pr.Id,
                    Title = pr.Title,
                    Description = pr.Description,
                    SourceBranch = pr.SourceBranch,
                    TargetBranch = pr.TargetBranch,
                    Status = pr.Status,
                    CreatedAt = pr.CreatedAt,
                    UpdatedAt = pr.UpdatedAt,
                    AuthorId = pr.AuthorId,
                    RepositoryId = pr.RepositoryId
                }).ToList(),
                RecentCommits = commits.Select(c => new CommitResponseDto
                {
                    Id = c.Id,
                    Message = c.Message,
                    AuthorId = c.AuthorId,
                    RepositoryId = c.RepositoryId,
                    CommitHash = c.CommitHash,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                Metrics = new EngineerMetricsDto
                {
                    TotalReviewsGiven = reviewsGiven,
                    AverageScoreReceived = reviews.Where(r => r.Score.HasValue).Any()
                        ? reviews.Where(r => r.Score.HasValue).Average(r => r.Score!.Value)
                        : 0
                }
            };

            return details;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engineer details for ID {EngineerId} and period {Period}", id, period);
            throw;
        }
    }

    public async Task<List<LeaderboardTrendDto>> GetLeaderboardTrendsAsync(string period = "MONTHLY", string? engineerId = null)
    {
        try
        {
            var trends = new List<LeaderboardTrendDto>();
            var periodsToGet = GetTrendPeriods(period);

            foreach (var trendPeriod in periodsToGet)
            {
                var (startDate, endDate) = ParseSpecificPeriod(trendPeriod);

                var engineers = await _context.Engineers
                    .Where(e => e.IsActive && (engineerId == null || e.Id == engineerId))
                    .ToListAsync();

                var engineerTrends = new List<EngineerTrendDto>();

                foreach (var engineer in engineers)
                {
                    var commits = await _context.Commits
                        .CountAsync(c => c.AuthorId == engineer.Id && c.CreatedAt >= startDate && c.CreatedAt <= endDate);

                    var pullRequests = await _context.PullRequests
                        .CountAsync(pr => pr.AuthorId == engineer.Id && pr.CreatedAt >= startDate && pr.CreatedAt <= endDate);

                    var reviews = await _context.Reviews
                        .Where(r => r.PullRequest.AuthorId == engineer.Id && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                        .ToListAsync();

                    var averageScore = reviews.Where(r => r.Score.HasValue).Any()
                        ? reviews.Where(r => r.Score.HasValue).Average(r => r.Score!.Value)
                        : 0;

                    engineerTrends.Add(new EngineerTrendDto
                    {
                        Engineer = new EngineerResponseDto
                        {
                            Id = engineer.Id,
                            Name = engineer.Name,
                            Email = engineer.Email,
                            AvatarUrl = engineer.AvatarUrl,
                            JoinedAt = engineer.JoinedAt,
                            IsActive = engineer.IsActive
                        },
                        Stats = new LeaderboardStatsDto
                        {
                            TotalCommits = commits,
                            TotalPullRequests = pullRequests,
                            TotalReviews = reviews.Count,
                            AverageReviewScore = averageScore,
                            Period = trendPeriod
                        }
                    });
                }

                trends.Add(new LeaderboardTrendDto
                {
                    Period = trendPeriod,
                    Engineers = engineerTrends,
                    Totals = new TotalStatsDto
                    {
                        Commits = engineerTrends.Sum(e => e.Stats.TotalCommits),
                        PullRequests = engineerTrends.Sum(e => e.Stats.TotalPullRequests),
                        AverageQuality = engineerTrends.Any() ? engineerTrends.Average(e => e.Stats.AverageReviewScore) : 0
                    }
                });
            }

            return trends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard trends for period {Period}, engineerId {EngineerId}", period, engineerId);
            throw;
        }
    }

    private (DateTime startDate, DateTime endDate) ParsePeriod(string period)
    {
        var endDate = DateTime.UtcNow;
        DateTime startDate;

        switch (period.ToUpper())
        {
            case "WEEKLY":
                startDate = endDate.AddDays(-7);
                break;
            case "MONTHLY":
                startDate = endDate.AddDays(-30);
                break;
            case "QUARTERLY":
                startDate = endDate.AddDays(-90);
                break;
            case "YEARLY":
                startDate = endDate.AddYears(-1);
                break;
            default:
                startDate = endDate.AddDays(-30);
                break;
        }

        return (startDate, endDate);
    }

    private (DateTime startDate, DateTime endDate) ParseSpecificPeriod(string period)
    {
        // This would parse specific periods like "2024-01", "2024-Q1", etc.
        // For now, just use current period logic
        return ParsePeriod("MONTHLY");
    }

    private List<string> GetTrendPeriods(string period)
    {
        // Generate last few periods for trending
        var periods = new List<string>();
        var current = DateTime.UtcNow;

        for (int i = 0; i < 6; i++)
        {
            switch (period.ToUpper())
            {
                case "MONTHLY":
                    periods.Add(current.AddMonths(-i).ToString("yyyy-MM"));
                    break;
                case "QUARTERLY":
                    var quarter = (current.AddMonths(-i * 3).Month - 1) / 3 + 1;
                    periods.Add($"{current.AddMonths(-i * 3).Year}-Q{quarter}");
                    break;
                default:
                    periods.Add(current.AddDays(-i * 7).ToString("yyyy-MM-dd"));
                    break;
            }
        }

        return periods;
    }

    private async Task<List<LeaderboardStatsDto>> GetHistoricalStatsAsync(string engineerId, string period)
    {
        var stats = new List<LeaderboardStatsDto>();
        var periods = GetTrendPeriods(period);

        foreach (var histPeriod in periods)
        {
            var (startDate, endDate) = ParseSpecificPeriod(histPeriod);

            var commits = await _context.Commits
                .CountAsync(c => c.AuthorId == engineerId && c.CreatedAt >= startDate && c.CreatedAt <= endDate);

            var pullRequests = await _context.PullRequests
                .CountAsync(pr => pr.AuthorId == engineerId && pr.CreatedAt >= startDate && pr.CreatedAt <= endDate);

            var reviews = await _context.Reviews
                .Where(r => r.PullRequest.AuthorId == engineerId && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .ToListAsync();

            var averageScore = reviews.Where(r => r.Score.HasValue).Any()
                ? reviews.Where(r => r.Score.HasValue).Average(r => r.Score!.Value)
                : 0;

            stats.Add(new LeaderboardStatsDto
            {
                TotalCommits = commits,
                TotalPullRequests = pullRequests,
                TotalReviews = reviews.Count,
                AverageReviewScore = averageScore,
                Period = histPeriod
            });
        }

        return stats;
    }
}
