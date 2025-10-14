namespace backend_dotnet.Models
{
    public class RepositoryIntegration
    {
        public string? Id { get; set; }
        public string? RepoId { get; set; }
        public string? Provider { get; set; } // github, gitlab, bitbucket
        public string? Token { get; set; }
        public string? BaseUrl { get; set; }
    }
}
