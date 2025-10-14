using Microsoft.EntityFrameworkCore;
using Serilog;
using CodePulseApi.Data;
using CodePulseApi.Services;
using AutoMapper;
using backend_dotnet.Services;
using backend_dotnet.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework
builder.Services.AddDbContext<CodePulseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration["Frontend:Url"] ?? "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register services
builder.Services.AddScoped<IAzureAIFoundryService, AzureAIFoundryService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
// Temporarily commented out until DTOs and models are fixed
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
//builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Add HTTP client for external services
builder.Services.AddHttpClient();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "CodePulse API",
        Version = "v1",
        Description = "API for CodePulse AI - Azure DevOps Integration with AI Code Reviews"
    });
});

// JWT config
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-very-secure-secret";
var jwtExpiryMinutes = int.TryParse(builder.Configuration["Jwt:ExpiryMinutes"], out var exp) ? exp : 60;

// Register CosmosClient
builder.Services.AddSingleton(s => new CosmosClient(builder.Configuration["CosmosDb:ConnectionString"]));

// Register AuthService
builder.Services.AddScoped<AuthService>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    return new AuthService(
        cosmosClient,
        builder.Configuration["CosmosDb:DatabaseName"] ?? "CodePulseDb",
        builder.Configuration["CosmosDb:UserContainer"] ?? "Users",
        jwtSecret,
        jwtExpiryMinutes
    );
});

// Add JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodePulse API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication(); // <-- Add this before authorization
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new
{
    status = "OK",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
});

try
{
    Log.Information("Starting CodePulse API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
