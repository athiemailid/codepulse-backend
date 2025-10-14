using Azure;
using Azure.AI.Inference;
using CodePulseApi.DTOs;
using System.Text.Json;

namespace CodePulseApi.Services;

public class AzureAIFoundryService : IAzureAIFoundryService
{
    private readonly ChatCompletionsClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureAIFoundryService> _logger;
    private readonly string _modelName;

    public AzureAIFoundryService(IConfiguration configuration, ILogger<AzureAIFoundryService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var endpoint = _configuration["AzureAIFoundry:Endpoint"] ?? throw new InvalidOperationException("Azure AI Foundry endpoint not configured");
        var apiKey = _configuration["AzureAIFoundry:ApiKey"] ?? throw new InvalidOperationException("Azure AI Foundry API key not configured");
        _modelName = _configuration["AzureAIFoundry:ModelName"] ?? "meta-llama-3-1-70b-instruct";

        _client = new ChatCompletionsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<CodeReviewResultDto> ReviewCodeAsync(string code, string fileName, string? pullRequestContext = null)
    {
        try
        {
            var prompt = BuildCodeReviewPrompt(code, fileName, pullRequestContext);

            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestSystemMessage("You are an expert code reviewer. Analyze the provided code and give constructive feedback focusing on code quality, best practices, potential bugs, security issues, and performance improvements."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 2000,
                Temperature = 0.3f,
                Model = _modelName
            };

            var response = await _client.CompleteAsync(requestOptions);
            var content = response.Value.Choices[0].Message.Content;

            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException("No response from Azure AI Foundry");
            }

            return ParseAIResponse(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AI Foundry error during code review");
            throw new InvalidOperationException("Failed to analyze code with AI", ex);
        }
    }

    public async Task<string> SummarizePullRequestAsync(PullRequestSummaryDto pullRequest)
    {
        try
        {
            var prompt = BuildPullRequestSummaryPrompt(pullRequest);

            var requestOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestSystemMessage("You are a technical writer who creates clear, concise summaries of code changes."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 500,
                Temperature = 0.3f,
                Model = _modelName
            };

            var response = await _client.CompleteAsync(requestOptions);
            return response.Value.Choices[0].Message.Content ?? "Summary not available";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to summarize pull request");
            return "Summary not available";
        }
    }

    private string BuildCodeReviewPrompt(string code, string fileName, string? pullRequestContext)
    {
        var prompt = $@"
Please review the following code file: {fileName}

{(pullRequestContext != null ? $"Pull Request Context: {pullRequestContext}" : "")}

Code to review:
```
{code}
```

Please provide a structured review in JSON format with the following structure:
{{
  ""score"": <number between 0-10, where 10 is excellent>,
  ""feedback"": ""<overall feedback summary>"",
  ""suggestions"": [""<suggestion 1>"", ""<suggestion 2>"", ...],
  ""issues"": [
    {{
      ""line"": <line number>,
      ""severity"": ""<low|medium|high>"",
      ""message"": ""<issue description>"",
      ""suggestion"": ""<how to fix>""
    }}
  ]
}}

Focus on:
- Code quality and readability
- Performance optimizations
- Security vulnerabilities
- Best practices
- Potential bugs
- Design patterns
- Error handling
";

        return prompt;
    }

    private string BuildPullRequestSummaryPrompt(PullRequestSummaryDto pullRequest)
    {
        var commitsInfo = string.Join("\n", pullRequest.Commits.Select(c => 
            $"- {c.Message} (by {c.Author}, {c.ChangedFiles} files changed)"));

        var prompt = $@"
Summarize this pull request:

Title: {pullRequest.Title}
Description: {pullRequest.Description ?? "No description provided"}

Commits:
{commitsInfo}

Provide a concise summary highlighting the main changes and their impact.
";

        return prompt;
    }

    private CodeReviewResultDto ParseAIResponse(string content)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var parsed = JsonSerializer.Deserialize<JsonElement>(jsonContent, options);
                
                return new CodeReviewResultDto
                {
                    Score = Math.Max(0, Math.Min(10, parsed.TryGetProperty("score", out var scoreElement) ? scoreElement.GetDouble() : 5)),
                    Feedback = parsed.TryGetProperty("feedback", out var feedbackElement) ? feedbackElement.GetString() ?? "Code review completed" : "Code review completed",
                    Suggestions = parsed.TryGetProperty("suggestions", out var suggestionsElement) && suggestionsElement.ValueKind == JsonValueKind.Array 
                        ? suggestionsElement.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                        : new List<string>(),
                    Issues = parsed.TryGetProperty("issues", out var issuesElement) && issuesElement.ValueKind == JsonValueKind.Array
                        ? issuesElement.EnumerateArray().Select(ParseIssue).Where(i => i != null).Cast<CodeIssueDto>().ToList()
                        : new List<CodeIssueDto>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI response");
        }

        // Fallback response
        return new CodeReviewResultDto
        {
            Score = 5,
            Feedback = content.Length > 500 ? content.Substring(0, 500) : content,
            Suggestions = new List<string>(),
            Issues = new List<CodeIssueDto>()
        };
    }

    private CodeIssueDto? ParseIssue(JsonElement issueElement)
    {
        try
        {
            return new CodeIssueDto
            {
                Line = issueElement.TryGetProperty("line", out var lineElement) ? lineElement.GetInt32() : 0,
                Severity = issueElement.TryGetProperty("severity", out var severityElement) ? severityElement.GetString() ?? "low" : "low",
                Message = issueElement.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "",
                Suggestion = issueElement.TryGetProperty("suggestion", out var suggestionElement) ? suggestionElement.GetString() ?? "" : ""
            };
        }
        catch
        {
            return null;
        }
    }
}
