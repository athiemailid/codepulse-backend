# CodePulse AI - Azure AI Foundry Integration

## Overview

CodePulse AI is a full-stack application that integrates with Azure DevOps to provide AI-powered code reviews and engineering analytics. This version uses **Azure AI Foundry** (Azure AI Studio) foundation models instead of Azure OpenAI.

## Azure AI Foundry vs Azure OpenAI

### What is Azure AI Foundry?
Azure AI Foundry (formerly Azure AI Studio) provides access to various foundation models including:
- **Meta Llama 3.1** (70B, 8B, 405B parameters)
- **Mistral models** (7B, 22B)
- **Cohere Command models**
- **Phi-3 models** from Microsoft
- **JAIS models** (Arabic language)

### Benefits of Azure AI Foundry
1. **Cost-effective**: Often more affordable than Azure OpenAI
2. **Diverse model options**: Access to multiple model families
3. **Specialized models**: Models optimized for specific use cases
4. **Open source models**: Access to open-source foundation models
5. **Flexible deployment**: Deploy models in your own Azure subscription

## Setup Instructions

### 1. Azure AI Foundry Setup

#### Step 1: Create Azure AI Hub
```bash
# Using Azure CLI
az ml workspace create \
  --resource-group myresourcegroup \
  --workspace-name myaihub \
  --kind Hub
```

#### Step 2: Deploy a Foundation Model
1. Go to Azure AI Studio (https://ai.azure.com/)
2. Navigate to **Model Catalog**
3. Choose a model (recommended: **Meta Llama 3.1 70B Instruct**)
4. Click **Deploy** → **Serverless API**
5. Configure deployment settings
6. Note the **Endpoint URL** and **API Key**

#### Step 3: Get Model Details
- **Endpoint**: `https://your-workspace-name.region.inference.ai.azure.com/`
- **API Key**: Available in the deployment details
- **Model Name**: `meta-llama-3-1-70b-instruct` (or your chosen model)

### 2. Configure CodePulse API

#### Update appsettings.json
```json
{
  "AzureAIFoundry": {
    "Endpoint": "https://your-workspace-name.eastus.inference.ai.azure.com/",
    "ApiKey": "your-azure-ai-foundry-api-key",
    "ModelName": "meta-llama-3-1-70b-instruct",
    "DeploymentName": "your-deployment-name"
  }
}
```

#### Environment Variables (Alternative)
```bash
# For production, use environment variables
export AZUREAI_FOUNDRY_ENDPOINT="https://your-workspace-name.eastus.inference.ai.azure.com/"
export AZUREAI_FOUNDRY_APIKEY="your-api-key"
export AZUREAI_FOUNDRY_MODEL="meta-llama-3-1-70b-instruct"
```

### 3. Database Setup

#### Create Database
```bash
# Create SQL Server LocalDB database
dotnet ef database update
```

#### Connection String
Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CodePulseAi;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

## Available Foundation Models

### Recommended Models for Code Review

#### 1. Meta Llama 3.1 70B Instruct (Recommended)
- **Model ID**: `meta-llama-3-1-70b-instruct`
- **Best for**: Complex code analysis, detailed feedback
- **Context length**: 128k tokens
- **Languages**: Excellent for most programming languages

#### 2. Meta Llama 3.1 8B Instruct
- **Model ID**: `meta-llama-3-1-8b-instruct`
- **Best for**: Fast code reviews, cost-effective
- **Context length**: 128k tokens
- **Languages**: Good for most programming languages

#### 3. Mistral Large
- **Model ID**: `mistral-large`
- **Best for**: Multi-language code analysis
- **Context length**: 32k tokens
- **Languages**: Excellent for European languages + code

#### 4. Cohere Command R+
- **Model ID**: `cohere-command-r-plus`
- **Best for**: Retrieval-augmented code reviews
- **Context length**: 128k tokens
- **Languages**: Good for English-focused codebases

### Model Comparison

| Model | Parameters | Cost | Speed | Code Quality | Best Use Case |
|-------|------------|------|-------|--------------|---------------|
| Llama 3.1 70B | 70B | Medium | Medium | Excellent | Detailed reviews |
| Llama 3.1 8B | 8B | Low | Fast | Good | Quick reviews |
| Mistral Large | ~70B | Medium | Medium | Very Good | Multi-language |
| Cohere Command R+ | ~104B | High | Medium | Excellent | RAG-enhanced |

## API Endpoints

### 1. Webhook Endpoint
```http
POST /api/webhook/azure-devops
Content-Type: application/json

{
  "eventType": "git.push",
  "resource": {
    "commits": [...],
    "repository": {...}
  }
}
```

### 2. Manual Code Review
```http
POST /api/review/analyze
Content-Type: application/json

{
  "code": "public class Example { ... }",
  "fileName": "Example.cs",
  "context": "Pull request adding new feature"
}
```

### 3. Health Check
```http
GET /health
```

Response:
```json
{
  "status": "OK",
  "timestamp": "2025-08-24T12:44:08.123Z",
  "version": "1.0.0"
}
```

## Code Review Features

### AI-Powered Analysis
The Azure AI Foundry integration provides:

1. **Code Quality Scoring** (0-10 scale)
2. **Security Vulnerability Detection**
3. **Performance Optimization Suggestions**
4. **Best Practices Recommendations**
5. **Bug Detection and Prevention**

### Sample Response Format
```json
{
  "score": 8.5,
  "feedback": "Good code structure with proper error handling. Consider adding input validation.",
  "suggestions": [
    "Add null checks for input parameters",
    "Consider using async/await for database operations",
    "Extract magic numbers to constants"
  ],
  "issues": [
    {
      "line": 15,
      "severity": "medium",
      "message": "Potential null reference exception",
      "suggestion": "Add null check: if (input == null) throw new ArgumentNullException()"
    }
  ]
}
```

## Pricing Comparison

### Azure AI Foundry (Estimated Monthly Costs)
- **Llama 3.1 8B**: ~$30-50/month for 10k reviews
- **Llama 3.1 70B**: ~$80-120/month for 10k reviews
- **Mistral Large**: ~$100-150/month for 10k reviews

### Azure OpenAI (Comparison)
- **GPT-3.5-turbo**: ~$60-90/month for 10k reviews
- **GPT-4**: ~$300-500/month for 10k reviews

*Note: Actual costs depend on usage patterns, prompt length, and response length.*

## Deployment Options

### 1. Development Environment
```bash
cd backend-dotnet
dotnet run
```
Access: `http://localhost:5200`

### 2. Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CodePulseApi.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodePulseApi.dll"]
```

### 3. Azure App Service
```yaml
# azure-pipelines.yml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: DotNetCoreCLI@2
  inputs:
    command: 'publish'
    publishWebProjects: true
    arguments: '--configuration Release --output $(Build.ArtifactStagingDirectory)'

- task: AzureWebApp@1
  inputs:
    azureSubscription: 'your-subscription'
    appName: 'codepulse-api'
    package: '$(Build.ArtifactStagingDirectory)/**/*.zip'
```

## Best Practices

### 1. Model Selection
- **Start with Llama 3.1 8B** for testing and development
- **Upgrade to Llama 3.1 70B** for production
- **Use Mistral Large** for multilingual codebases
- **Consider Cohere Command R+** for large context needs

### 2. Prompt Optimization
- Keep code snippets under 4000 characters for faster processing
- Include relevant context (PR description, file purpose)
- Use consistent prompt templates for better results

### 3. Error Handling
- Implement retry logic for API calls
- Cache results to reduce API calls
- Provide fallback responses for API failures

### 4. Security
- Store API keys in Azure Key Vault (production)
- Use managed identities when possible
- Implement rate limiting to prevent abuse

## Troubleshooting

### Common Issues

#### 1. "Model not found" Error
```json
{
  "error": "The model 'meta-llama-3-1-70b-instruct' was not found"
}
```
**Solution**: Verify the model name in your deployment matches the configuration.

#### 2. Authentication Failed
```json
{
  "error": "Authentication failed"
}
```
**Solution**: Check your API key and endpoint URL in the configuration.

#### 3. Rate Limiting
```json
{
  "error": "Rate limit exceeded"
}
```
**Solution**: Implement exponential backoff retry logic.

### Monitoring and Logging

#### Application Insights Integration
```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

#### Custom Metrics
```csharp
// Track AI service usage
telemetryClient.TrackMetric("AIFoundry.ReviewsGenerated", 1);
telemetryClient.TrackDuration("AIFoundry.ResponseTime", responseTime);
```

## Migration from Azure OpenAI

If you're migrating from Azure OpenAI:

1. **Update NuGet packages**:
   ```bash
   dotnet remove package Azure.AI.OpenAI
   dotnet add package Azure.AI.Inference --version 1.0.0-beta.1
   ```

2. **Update configuration**:
   - Replace `AzureOpenAI` section with `AzureAIFoundry`
   - Update endpoint format
   - Change model names

3. **Update service implementation**:
   - Replace `AzureOpenAIClient` with `ChatCompletionsClient`
   - Update API call methods
   - Adjust response parsing if needed

## Support and Resources

- **Azure AI Studio Documentation**: https://docs.microsoft.com/azure/ai-studio/
- **Model Catalog**: https://ai.azure.com/explore/models
- **Pricing Calculator**: https://azure.microsoft.com/pricing/calculator/
- **GitHub Repository**: https://github.com/your-repo/codepulse-ai

## License

This project is licensed under the MIT License.
