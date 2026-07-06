using System.Globalization;
using System.Text.Json;
using FHOOE.Freydis.Agents.Agents.DigitalTwin.Services;
using FHOOE.Freydis.GraphQLServer.Configuration;
using FHOOE.Freydis.GraphQLServer.Extensions;
using FHOOE.Freydis.GraphQLServer.Services;
using FHOOE.Freydis.Infrastructure.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
var runId = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "Freydis")
    .Enrich.WithProperty("RunId", runId)
    .WriteTo.Async(sink => sink.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture))
    .WriteTo.Async(sink => sink.File(
        new CompactJsonFormatter(),
        $"logs/runs/run-{runId}.json",
        rollingInterval: RollingInterval.Infinite,
        retainedFileCountLimit: null,
        buffered: true,
        flushToDiskInterval: TimeSpan.FromSeconds(5)))
    .WriteTo.Async(sink => sink.File(
        new CompactJsonFormatter(),
        "logs/freydis-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        buffered: true,
        flushToDiskInterval: TimeSpan.FromSeconds(5)))
    .CreateLogger();

builder.Host.UseSerilog();

// Configure logging - let appsettings.json control the levels

// Add services to the container using extension methods
builder.Services
    .AddConfigurationValidation(builder.Configuration)
    .AddPostgresPersistence()
    .AddRepositoryCaching()
    .AddApplicationServices(builder.Configuration)
    .AddAgentSynchronizationServices()
    .AddAgentServices(builder.Configuration)
    .AddOrchestrationServices()
    .AddGraphQlServices(builder.Configuration)
    .AddCorsConfiguration(builder.Configuration)
    .AddApplicationHealthChecks();

// Add hosted services
// Startup validation MUST run first to catch configuration issues early
builder.Services.AddHostedService<StartupValidationService>();

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

app.UseCors("AllowFrontend");
app.UseWebSockets();

// Map endpoints
app.MapGraphQL();

// Digital Twin WebSocket endpoint (only when Digital Twin agents are enabled)
var agentsConfig = app.Configuration.GetSection("Agents").Get<AgentsConfiguration>();
if (agentsConfig?.DigitalTwin.Enabled != false)
    app.Map("/ws/digital-twin", async (HttpContext context, DigitalTwinWebSocketHandler handler) =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket connections only");
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await handler.HandleConnectionAsync(webSocket, context.RequestAborted);
    });

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

// Simple status endpoint for monitoring systems
app.MapHealthChecks("/status", new HealthCheckOptions
{
    Predicate = _ => false, // Don't run any health checks, just return overall status
    ResponseWriter = async (context, _) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        }));
    }
});

// Run with GraphQL commands support
try
{
    Log.Information("Starting Freydis GraphQL Server");
    Log.Debug("RunId: {RunId}, LogFile: logs/runs/run-{RunId}.json", runId);
    app.RunWithGraphQLCommands(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down Freydis GraphQL Server");
    Log.CloseAndFlush();
}