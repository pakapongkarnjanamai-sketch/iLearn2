using iLearn.Application.Interfaces.Services;
using iLearn.Application.Middleware;
using iLearn.Application.Services;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

// เพิ่ม Session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Windows Authentication Configuration
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// IIS Integration for Windows Authentication
builder.Services.Configure<IISOptions>(options =>
{
    options.AutomaticAuthentication = true;
    options.AuthenticationDisplayName = "Windows";
});
// ในส่วนการลงทะเบียน Services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // เมื่อไม่มีสิทธิ์ หรือยังไม่ได้ Login ให้เด้งไปที่ HomeController Action Index (หน้าแรก)
        options.LoginPath = "/Home/Index";
        options.AccessDeniedPath = "/Home/Index";

        // กำหนดระยะเวลาของ Cookie (เช่น 30 นาที)
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });


// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IApiUserService, ApiUserService>();
builder.Services.AddHttpClient<IStudentApiService, StudentApiService>();

// HTTP Client for API calls
builder.Services.AddHttpClient("iLearnAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiSettings:BaseUrl") ?? "https://localhost:7128");
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseDefaultCredentials = true
});

// Add memory cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Static files ก่อน

app.UseRouting();
app.UseSession(); // เพิ่ม Session

app.UseAuthentication();
app.UseAuthorization();

// Middleware หลัง authentication/authorization
app.UseMiddleware<ApiUserSyncMiddleware>();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
