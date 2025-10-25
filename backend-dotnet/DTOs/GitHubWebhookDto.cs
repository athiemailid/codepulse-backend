using System.Text.Json.Serialization;

namespace CodePulseApi.DTOs
{
    public class GitHubWebhookDto
    {
        public string? Ref { get; set; }
        public string? Before { get; set; }
        public string? After { get; set; }
        public RepositoryDto? Repository { get; set; }
        public PusherDto? Pusher { get; set; }
        public SenderDto? Sender { get; set; }
        public List<CommitDto>? Commits { get; set; }
        public CommitDto? HeadCommit { get; set; }
        public bool Created { get; set; }
        public bool Deleted { get; set; }
        public bool Forced { get; set; }
        public string? BaseRef { get; set; }
        public string? Compare { get; set; }
        public string? Action { get; set; } // Optional field for event type
    }

    public class RepositoryDto
    {
        public long Id { get; set; }
        public string? NodeId { get; set; }
        public string? Name { get; set; }
        public string? FullName { get; set; }
        public bool Private { get; set; }
        public OwnerDto? Owner { get; set; }
        public string? HtmlUrl { get; set; }
        public string? Description { get; set; }
        public bool Fork { get; set; }
        public string? Url { get; set; }
        public string? Language { get; set; }
        public int ForksCount { get; set; }
        public int OpenIssuesCount { get; set; }
        public int WatchersCount { get; set; }
        public string? DefaultBranch { get; set; }
    }

    public class OwnerDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Login { get; set; }
        public long Id { get; set; }
        public string? NodeId { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Url { get; set; }
        public bool SiteAdmin { get; set; }
    }

    public class PusherDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    public class SenderDto
    {
        public string? Login { get; set; }
        public long Id { get; set; }
        public string? NodeId { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Url { get; set; }
        public bool SiteAdmin { get; set; }
    }

    public class CommitDto
    {
        public string? Id { get; set; }
        public string? TreeId { get; set; }
        public bool Distinct { get; set; }
        public string? Message { get; set; }
        public string? Timestamp { get; set; }
        public string? Url { get; set; }
        public AuthorCommitterDto? Author { get; set; }
        public AuthorCommitterDto? Committer { get; set; }
        public List<string>? Added { get; set; }
        public List<string>? Removed { get; set; }
        public List<string>? Modified { get; set; }
    }

    public class AuthorCommitterDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
    }
}
