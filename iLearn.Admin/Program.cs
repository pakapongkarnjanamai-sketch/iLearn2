using iLearn.Admin.Middleware;
using iLearn.Admin.Services;
using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl") ?? "https://localhost:7128/api";

builder.Services.AddRazorPages().AddJsonOptions(ConfigureJsonOptions);
builder.Services.AddControllers().AddJsonOptions(ConfigureJsonOptions);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.Configure<IISOptions>(options =>
{
    options.AutomaticAuthentication = true;
    options.AuthenticationDisplayName = "Windows";
});

builder.Services.AddAuthorization(options =>
{
    var adminOnlyPolicy = CreateAdminOnlyPolicy();

    options.DefaultPolicy = adminOnlyPolicy;
    options.FallbackPolicy = adminOnlyPolicy;

    options.AddPolicy("AdminOnly", adminOnlyPolicy);

    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("SuperAdmin"));

    options.AddPolicy("DomainUser", policy =>
        policy.RequireAssertion(context =>
            context.User.Identity?.Name?.StartsWith("NIKONOA\\", StringComparison.OrdinalIgnoreCase) == true));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IApiUserService, ApiUserService>();

builder.Services.AddHttpClient("iLearnAPI", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseDefaultCredentials = true
});

builder.Services.AddMemoryCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseMiddleware<ApiUserSyncMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void ConfigureJsonOptions(JsonOptions options)
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
}

static AuthorizationPolicy CreateAdminOnlyPolicy()
{
    return new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Admin", "SuperAdmin")
        .Build();
}
