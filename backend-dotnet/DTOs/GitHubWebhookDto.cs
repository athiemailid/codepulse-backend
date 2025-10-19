namespace CodePulseApi.DTOs
{
    public class GitHubWebhookDto
    {
        public string EventType { get; set; }
        public object Payload { get; set; }
    }
}
