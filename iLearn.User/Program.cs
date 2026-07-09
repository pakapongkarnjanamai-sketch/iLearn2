using iLearn.User.Extensions;
using iLearn.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

ValidateRequiredSecrets(builder.Configuration, builder.Environment);

// ── MVC / Razor Pages ──
builder.Services.AddRazorPages()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ── Session ──
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ── Authentication ──
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.AccessDeniedPath = "/Home/Index";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = RedirectAjaxToSessionExpiredAsync;
        options.Events.OnRedirectToAccessDenied = RedirectAjaxToSessionExpiredAsync;
    });

builder.Services.Configure<IISOptions>(options =>
{
    options.AutomaticAuthentication = false;
    options.AuthenticationDisplayName = "Windows";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("LearnerSession", policy =>
    {
        policy.AuthenticationSchemes.Add(CookieAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ClaimTypes.NameIdentifier);
    });
});

// ── Application Services ──
builder.Services.AddUserServices(builder.Configuration);

var app = builder.Build();

// ── Middleware Pipeline ──
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCourseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void ValidateRequiredSecrets(IConfiguration configuration, IHostEnvironment environment)
{
    EnsureConfigured(
        configuration,
        $"{LearnerProxyAuthOptions.SectionName}:SharedSecret",
        "learner proxy shared secret",
        environment);
}

static Task RedirectAjaxToSessionExpiredAsync(RedirectContext<CookieAuthenticationOptions> context)
{
    if (!IsAjaxRequest(context.Request))
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    context.Response.StatusCode = 440;
    context.Response.ContentType = "application/json; charset=utf-8";
    return context.Response.WriteAsJsonAsync(new
    {
        success = false,
        sessionExpired = true,
        message = "Session หมดอายุ กรุณาเข้าสู่ระบบอีกครั้ง",
        redirectUrl = context.RedirectUri
    });
}

static bool IsAjaxRequest(HttpRequest request)
{
    return string.Equals(
            request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase)
        || request.Headers.Accept.Any(value =>
            value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
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

static class CourseStaticFileApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCourseStaticFiles(this WebApplication app)
    {
        var settings = app.Configuration
            .GetSection(nameof(FileSettings))
            .Get<FileSettings>() ?? new FileSettings();

        var courseFolder = string.IsNullOrWhiteSpace(settings.CourseFolder)
            ? "Courses"
            : settings.CourseFolder.Trim('/', '\\');

        var coursePhysicalPath = !string.IsNullOrWhiteSpace(settings.HostUnc)
            ? settings.FileUnc
            : Path.Combine(app.Environment.ContentRootPath, courseFolder);

        iLearn.User.Services.CourseContentStatus.PhysicalPath = coursePhysicalPath;
        iLearn.User.Services.CourseContentStatus.RequestPath = $"/{courseFolder.Replace('\\', '/')}";

        if (!Directory.Exists(coursePhysicalPath))
        {
            app.Logger.LogWarning(
                "Course static file folder does not exist: {CoursePhysicalPath}",
                coursePhysicalPath);
            return app;
        }

        iLearn.User.Services.CourseContentStatus.MountedAtStartup = true;

        return app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(coursePhysicalPath),
            RequestPath = $"/{courseFolder.Replace('\\', '/')}",
            OnPrepareResponse = context =>
            {
                context.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            }
        });
    }
}

