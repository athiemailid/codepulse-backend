using CodePulseApi.Data;
using CodePulseApi.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CodePulseApi.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly CodePulseDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(CodePulseDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AnalyticsDataDto> GetDashboardAnalyticsAsync(string period = "30d")
    {
        try
        {
            var (startDate, endDate) = ParsePeriod(period);

            var analytics = new AnalyticsDataDto
            {
                Period = period,
                TotalRepositories = await _context.Repositories.CountAsync(r => r.IsActive),
                TotalEngineers = await _context.Engineers.CountAsync(e => e.IsActive),
                TotalPullRequests = await _context.PullRequests
                    .CountAsync(pr => pr.CreatedAt >= startDate && pr.CreatedAt <= endDate),
                TotalCommits = await _context.Commits
                    .CountAsync(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate),
                TotalReviews = await _context.Reviews
                    .CountAsync(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate),
                AverageReviewScore = await _context.Reviews
                    .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate && r.Score.HasValue)
                    .AverageAsync(r => r.Score ?? 0),
                TrendData = await GetTrendDataAsync(startDate, endDate),
                TopPerformers = await GetTopPerformersAsync(startDate, endDate),
                RepositoryStats = await GetRepositoryStatsAsync(startDate, endDate)
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard analytics for period {Period}", period);
            throw;
        }
    }

    public async Task<EngineerAnalyticsDto> GetEngineerAnalyticsAsync(string id, string period = "30d")
    {
        try
        {
            var engineer = await _context.Engineers
                .FirstOrDefaultAsync(e => e.Id == id);

            if (engineer == null)
                throw new ArgumentException($"Engineer with ID {id} not found");

            var (startDate, endDate) = ParsePeriod(period);

            var commits = await _context.Commits
                .Where(c => c.AuthorId == id && c.CreatedAt >= startDate && c.CreatedAt <= endDate)
                .ToListAsync();

            var pullRequests = await _context.PullRequests
                .Where(pr => pr.AuthorId == id && pr.CreatedAt >= startDate && pr.CreatedAt <= endDate)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Where(r => r.PullRequest.AuthorId == id && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .ToListAsync();

            var analytics = new EngineerAnalyticsDto
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
                Period = period,
                Metrics = new EngineerMetricsDetailDto
                {
                    TotalCommits = commits.Count,
                    TotalPullRequests = pullRequests.Count,
                    AverageReviewScore = reviews.Where(r => r.Score.HasValue).Any() 
                        ? reviews.Where(r => r.Score.HasValue).Average(r => r.Score!.Value) 
                        : 0,
                    CommitsByRepository = commits
                        .GroupBy(c => c.Repository.Name)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    DailyCommits = await GetDailyCommitsAsync(id, startDate, endDate)
                }
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engineer analytics for ID {EngineerId} and period {Period}", id, period);
            throw;
        }
    }

    public async Task<RepositoryAnalyticsDto> GetRepositoryAnalyticsAsync(string id, string period = "30d")
    {
        try
        {
            var repository = await _context.Repositories
                .FirstOrDefaultAsync(r => r.Id == id);

            if (repository == null)
                throw new ArgumentException($"Repository with ID {id} not found");

            var (startDate, endDate) = ParsePeriod(period);

            var commits = await _context.Commits
                .Where(c => c.RepositoryId == id && c.CreatedAt >= startDate && c.CreatedAt <= endDate)
                .ToListAsync();

            var pullRequests = await _context.PullRequests
                .Where(pr => pr.RepositoryId == id && pr.CreatedAt >= startDate && pr.CreatedAt <= endDate)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Where(r => r.PullRequest.RepositoryId == id && r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .ToListAsync();

            var analytics = new RepositoryAnalyticsDto
            {
                Repository = new RepositoryResponseDto
                {
                    Id = repository.Id,
                    Name = repository.Name,
                    Url = repository.Url,
                    DefaultBranch = repository.DefaultBranch,
                    IsActive = repository.IsActive,
                    CreatedAt = repository.CreatedAt
                },
                Period = period,
                Metrics = new RepositoryMetricsDto
                {
                    CommitCount = commits.Count,
                    PullRequestCount = pullRequests.Count,
                    ActiveContributors = commits.Select(c => c.AuthorId).Distinct().Count(),
                    AverageReviewScore = reviews.Where(r => r.Score.HasValue).Any()
                        ? reviews.Where(r => r.Score.HasValue).Average(r => r.Score!.Value)
                        : 0,
                    CommitTrends = await GetRepositoryCommitTrendsAsync(id, startDate, endDate)
                }
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository analytics for ID {RepositoryId} and period {Period}", id, period);
            throw;
        }
    }

    public async Task<TeamComparisonDto> GetTeamComparisonAsync(string period = "30d")
    {
        try
        {
            var (startDate, endDate) = ParsePeriod(period);

            var engineers = await _context.Engineers
                .Where(e => e.IsActive)
                .ToListAsync();

            var engineerComparisons = new List<EngineerComparisonDto>();

            foreach (var engineer in engineers)
            {
                var commits = await _context.Commits
                    .CountAsync(c => c.AuthorId == engineer.Id && c.CreatedAt >= startDate && c.CreatedAt <= endDate);

                var pullRequests = await _context.PullRequests
                    .CountAsync(pr => pr.AuthorId == engineer.Id && pr.CreatedAt >= startDate && pr.CreatedAt <= endDate);

                var averageScore = await _context.Reviews
                    .Where(r => r.PullRequest.AuthorId == engineer.Id && r.CreatedAt >= startDate && r.CreatedAt <= endDate && r.Score.HasValue)
                    .AverageAsync(r => r.Score ?? 0);

                engineerComparisons.Add(new EngineerComparisonDto
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
                    TotalCommits = commits,
                    TotalPullRequests = pullRequests,
                    AverageReviewScore = averageScore
                });
            }

            return new TeamComparisonDto
            {
                Period = period,
                Engineers = engineerComparisons.OrderByDescending(e => e.TotalCommits).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team comparison for period {Period}", period);
            throw;
        }
    }

    private (DateTime startDate, DateTime endDate) ParsePeriod(string period)
    {
        var endDate = DateTime.UtcNow;
        DateTime startDate;

        switch (period.ToLower())
        {
            case "7d":
                startDate = endDate.AddDays(-7);
                break;
            case "30d":
                startDate = endDate.AddDays(-30);
                break;
            case "90d":
                startDate = endDate.AddDays(-90);
                break;
            case "1y":
                startDate = endDate.AddYears(-1);
                break;
            default:
                startDate = endDate.AddDays(-30);
                break;
        }

        return (startDate, endDate);
    }

    private async Task<List<TrendDataDto>> GetTrendDataAsync(DateTime startDate, DateTime endDate)
    {
        var trends = new List<TrendDataDto>();
        var current = startDate.Date;

        while (current <= endDate.Date)
        {
            var nextDay = current.AddDays(1);
            
            var commits = await _context.Commits
                .CountAsync(c => c.CreatedAt >= current && c.CreatedAt < nextDay);

            trends.Add(new TrendDataDto
            {
                Date = current,
                Value = commits
            });

            current = nextDay;
        }

        return trends;
    }

    private async Task<List<EngineerResponseDto>> GetTopPerformersAsync(DateTime startDate, DateTime endDate)
    {
        var topPerformers = await _context.Engineers
            .Where(e => e.IsActive)
            .Select(e => new
            {
                Engineer = e,
                CommitCount = _context.Commits.Count(c => c.AuthorId == e.Id && c.CreatedAt >= startDate && c.CreatedAt <= endDate)
            })
            .OrderByDescending(x => x.CommitCount)
            .Take(5)
            .ToListAsync();

        return topPerformers.Select(tp => new EngineerResponseDto
        {
            Id = tp.Engineer.Id,
            Name = tp.Engineer.Name,
            Email = tp.Engineer.Email,
            AvatarUrl = tp.Engineer.AvatarUrl,
            JoinedAt = tp.Engineer.JoinedAt,
            IsActive = tp.Engineer.IsActive
        }).ToList();
    }

    private async Task<List<RepositoryStatsDto>> GetRepositoryStatsAsync(DateTime startDate, DateTime endDate)
    {
        var repoStats = await _context.Repositories
            .Where(r => r.IsActive)
            .Select(r => new RepositoryStatsDto
            {
                Repository = new RepositoryResponseDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Url = r.Url,
                    DefaultBranch = r.DefaultBranch,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt
                },
                CommitCount = _context.Commits.Count(c => c.RepositoryId == r.Id && c.CreatedAt >= startDate && c.CreatedAt <= endDate),
                PullRequestCount = _context.PullRequests.Count(pr => pr.RepositoryId == r.Id && pr.CreatedAt >= startDate && pr.CreatedAt <= endDate)
            })
            .Take(10)
            .ToListAsync();

        return repoStats;
    }

    private async Task<List<TrendDataDto>> GetDailyCommitsAsync(string engineerId, DateTime startDate, DateTime endDate)
    {
        var trends = new List<TrendDataDto>();
        var current = startDate.Date;

        while (current <= endDate.Date)
        {
            var nextDay = current.AddDays(1);
            
            var commits = await _context.Commits
                .CountAsync(c => c.AuthorId == engineerId && c.CreatedAt >= current && c.CreatedAt < nextDay);

            trends.Add(new TrendDataDto
            {
                Date = current,
                Value = commits
            });

            current = nextDay;
        }

        return trends;
    }

    private async Task<List<TrendDataDto>> GetRepositoryCommitTrendsAsync(string repositoryId, DateTime startDate, DateTime endDate)
    {
        var trends = new List<TrendDataDto>();
        var current = startDate.Date;

        while (current <= endDate.Date)
        {
            var nextDay = current.AddDays(1);
            
            var commits = await _context.Commits
                .CountAsync(c => c.RepositoryId == repositoryId && c.CreatedAt >= current && c.CreatedAt < nextDay);

            trends.Add(new TrendDataDto
            {
                Date = current,
                Value = commits
            });

            current = nextDay;
        }

        return trends;
    }
}
