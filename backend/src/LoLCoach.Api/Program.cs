using System.Reflection;
using FluentValidation;
using LoLCoach.Api.Analytics.Analyzers;
using LoLCoach.Api.Analytics.Metrics;
using LoLCoach.Api.Analytics.Recommendations;
using LoLCoach.Api.Application;
using LoLCoach.Api.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

const string CorsPolicyName = "AllowedOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>("postgresql", tags: ["ready"]);
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LoLCoach API",
        Version = "v1",
        Description = "API for LoLCoach player integrations."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
    options.SchemaFilter<SearchPlayerSchemaFilter>();
});

builder.Services.AddDbContext<PlayerDbContext>((services, options) =>
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("LoLCoach")
        ?? throw new InvalidOperationException("Configure ConnectionStrings__LoLCoach before using persistence.");
    options.UseNpgsql(connection);
});
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IMatchNormalizer, MatchNormalizer>();
builder.Services.AddScoped<IValidator<SearchPlayerCommand>, SearchPlayerValidator>();
builder.Services.AddScoped<SearchPlayerHandler>();
builder.Services.AddScoped<SyncPlayerMatchesHandler>();
builder.Services.AddSingleton<PlayerMetricsCalculator>();
builder.Services.AddSingleton<IPerformanceAnalyzer, FarmingAnalyzer>();
builder.Services.AddSingleton<IPerformanceAnalyzer, DeathAnalyzer>();
builder.Services.AddSingleton<IPerformanceAnalyzer, VisionAnalyzer>();
builder.Services.AddSingleton<IPerformanceAnalyzer, CombatAnalyzer>();
builder.Services.AddSingleton<IPerformanceAnalyzer, ConsistencyAnalyzer>();
builder.Services.AddSingleton<IPerformanceAnalyzer, ChampionAnalyzer>();
builder.Services.AddSingleton<RecommendationEngine>();
builder.Services.AddScoped<PerformanceAnalysisService>();
builder.Services.AddOptions<AiCoachOptions>()
    .BindConfiguration(AiCoachOptions.SectionName)
    .PostConfigure(options =>
    {
        options.GeminiApiKey ??= builder.Configuration["GEMINI_API_KEY"];
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<IRiotAccountClient, RiotAccountClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<IRiotMatchClient, RiotMatchClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<IAiCoach, GeminiAiCoach>((services, client) =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiCoachOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var corsAllowedOrigins = GetAllowedOrigins(builder);
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (corsAllowedOrigins.Length == 0)
        {
            // Non-development with no configured origins: allow none. Never AllowAnyOrigin.
            policy.SetIsOriginAllowed(_ => false);
        }
        else
        {
            policy.WithOrigins(corsAllowedOrigins);
        }

        policy.WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "Authorization", "Accept");
    });
});

var app = builder.Build();
if (app.Configuration.GetValue<bool>("ASPNETCORE_FORWARDEDHEADERS_ENABLED"))
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
}

app.UseExceptionHandler();

var swaggerEnabled = app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "LoLCoach API v1"));
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors(CorsPolicyName);
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponseAsync,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
});
app.MapControllers();
app.Map("/api/{**rest}", () => Results.Problem(
    statusCode: StatusCodes.Status404NotFound,
    title: "Not Found",
    type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"));
app.MapFallbackToFile("index.html");
app.Run();

static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new { status = report.Status.ToString().ToLowerInvariant() });
}

static string[] GetAllowedOrigins(WebApplicationBuilder builder)
{
    var configured = builder.Configuration["CORS:AllowedOrigins"];
    if (!string.IsNullOrWhiteSpace(configured))
    {
        return configured.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    return builder.Environment.IsDevelopment()
        ? ["http://localhost:4200", "http://127.0.0.1:4200"]
        : [];
}

public partial class Program { }
