using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PRGatekeeper.Agents;
using PRGatekeeper.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure logging
builder.Services.AddLogging(configure =>
{
    configure.ClearProviders();
    configure.AddConsole();
    if (builder.Environment.IsDevelopment())
    {
        configure.SetMinimumLevel(LogLevel.Debug);
    }
});

// Register state management
builder.Services.AddSingleton<IStateManager, InMemoryStateManager>();
builder.Services.AddSingleton<ExecutionTracker>();

// Register Agent Framework
builder.Services.AddSingleton<AgentFactory>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.MapControllers();

// Initialize agent factory on startup
var agentFactory = app.Services.GetRequiredService<AgentFactory>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("🚀 PR Gatekeeper Backend initialized with Microsoft Agent Framework 1.0 (GA)");
logger.LogInformation("📊 Agent-to-Agent collaboration enabled");
logger.LogInformation("🔄 Deterministic execution loops configured");

app.Run();
