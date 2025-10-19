using System.Text.Json;

namespace CodePulseApi.DTOs
{
    public class GitHubWebhookDto
    {
        public string Ref { get; set; }
        public string Before { get; set; }
        public string After { get; set; }
        public RepositoryDto Repository { get; set; }
        public PusherDto Pusher { get; set; }
        public SenderDto Sender { get; set; }
        public List<CommitDto> Commits { get; set; }
        public CommitDto HeadCommit { get; set; }

        // Property to represent the event type (e.g., "push", "pull_request")
        public string Action { get; set; }

        // Optional: Add a property to store the raw JSON payload
        public JsonElement Payload { get; set; }
    }

   

    public class PusherDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class SenderDto
    {
        public string Login { get; set; }
        public string AvatarUrl { get; set; }
    }

  
}
