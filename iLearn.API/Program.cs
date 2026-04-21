using iLearn.API.Extensions;
using iLearn.API.Hubs;
using iLearn.API.Middleware;
using iLearn.Application;
using iLearn.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();

