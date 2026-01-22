using System.Text;
using DotNetEnv;
using LogCopilot.Application.Interfaces;
using LogCopilot.Infrastructure.AI;
using LogCopilot.Infrastructure.Clustering;
using LogCopilot.Infrastructure.Data;
using LogCopilot.Infrastructure.Features;
using LogCopilot.Infrastructure.Integrations;
using LogCopilot.Infrastructure.Licensing;
using LogCopilot.Infrastructure.Plugins;
using LogCopilot.Infrastructure.Services;
using LogCopilot.Infrastructure.Storage;
using LogCopilot.Infrastructure.Tracing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
Env.Load();

builder.Configuration.AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/lc_.log", rollingInterval: RollingInterval.Hour)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<LogCopilotDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIngestionService, IngestionService>();
builder.Services.AddScoped<IClusterService, ClusterService>();
builder.Services.AddScoped<ITraceService, TraceService>();
builder.Services.AddScoped<IIncidentReportService, IncidentReportService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<ClusteringService>();
builder.Services.AddScoped<TraceBuilder>();

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Logging.AddConsole();

var aiProvider = builder.Configuration["AI:Provider"];
if (aiProvider == "OpenAI")
{
    builder.Services.AddHttpClient<IAIProvider, OpenAICompatibleProvider>();
}
else
{
    builder.Services.AddSingleton<IAIProvider, MockAIProvider>();
}

builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

var featureFlags = new FeatureFlagService(builder.Configuration);
var proEnabled = featureFlags.IsEnabled("ProEnabled");
var licenseKey =
    builder.Configuration["License:Key"]
    ?? builder.Configuration["LICENSE_KEY"]
    ?? Environment.GetEnvironmentVariable("LICENSE_KEY");
var hasValidLicense =
    !string.IsNullOrEmpty(licenseKey)
    && licenseKey.StartsWith("LC-PRO-")
    && licenseKey.Length >= 20;

if (proEnabled && hasValidLicense)
{
    builder.Services.AddSingleton<ILicenseVerifier, ProLicenseVerifier>();
    builder.Services.AddScoped<IIncidentNarrativeGenerator, ProviderAwareNarrativeGenerator>();
}
else
{
    builder.Services.AddSingleton<ILicenseVerifier, CommunityLicenseVerifier>();
    builder.Services.AddScoped<IIncidentNarrativeGenerator, CommunityHeuristicNarrativeGenerator>();
}

builder.Services.AddScoped<IIntegrationSink, NullIntegrationSink>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<LogCopilot.Infrastructure.Middleware.ApiKeyAuthenticationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LogCopilotDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        await BillingPlanSeeder.SeedBillingPlansAsync(db);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error running migrations or seeding");
    }
}

app.Run();
