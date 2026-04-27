using iLearn.API.Extensions;
using iLearn.API.Hubs;
using iLearn.API.Middleware;
using iLearn.API.Services;
using iLearn.Application;
using iLearn.Application.Common;
using iLearn.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

ValidateRequiredSecrets(builder.Configuration, builder.Environment);

// ── Presentation (controllers, JSON, SignalR, realtime notifier) ──
builder.Services.AddPresentation();

// ── Authentication & Authorization ──
builder.Services.AddApiAuthentication();
builder.Services.AddApiAuthorization(builder.Configuration);

// ── OpenAPI / Swagger ──
builder.Services.AddOpenApi();
builder.Services.AddApiSwagger();

// ── Cross-cutting infrastructure ──
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<LearnerProxyAuthOptions>(
    builder.Configuration.GetSection(LearnerProxyAuthOptions.SectionName));
builder.Services.AddScoped<ILearnerProxyIdentityResolver, LearnerProxyIdentityResolver>();

// ── Clean Architecture: Register layers via extension methods ──
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Development-only CORS ──
if (builder.Environment.IsDevelopment())
{
    using var bootstrapLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
    builder.Services.AddApiCors(
        builder.Configuration,
        bootstrapLoggerFactory.CreateLogger("Cors"));
}

var app = builder.Build();

// ── HTTP pipeline ──
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseApiSwagger();
    app.UseCors(CorsExtensions.DevelopmentPolicyName);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<ApiClaimsEnrichMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AdminActivityHub>("/hubs/admin-activity");
app.ValidateExplicitControllerAuthorizationPolicies();

app.Run();

static void ValidateRequiredSecrets(IConfiguration configuration, IHostEnvironment environment)
{
    EnsureConfigured(
        configuration,
        "ConnectionStrings:DefaultConnection",
        "SQL Server connection string",
        environment);

    EnsureConfigured(
        configuration,
        $"{LearnerProxyAuthOptions.SectionName}:SharedSecret",
        "learner proxy shared secret",
        environment);
}

static void EnsureConfigured(
    IConfiguration configuration,
    string key,
    string description,
    IHostEnvironment environment)
{
    var value = configuration[key];
    if (ConfigurationSecretGuard.HasRealValue(value))
        return;

    var scopeHint = environment.IsDevelopment()
        ? "dotnet user-secrets or environment variables"
        : "environment variables or your deployment secret store";

    throw new InvalidOperationException(
        $"Missing {description}. Configure '{key}' via {scopeHint}. Suggested environment variable name: '{ConfigurationSecretGuard.ToEnvironmentVariableName(key)}'.");
}

