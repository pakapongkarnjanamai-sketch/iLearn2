using iLearn.Application;
using iLearn.Application;
using iLearn.Infrastructure;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    // เพิ่มบรรทัดนี้เพื่อตัดวงจร Loop Reference
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));

    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("SuperAdmin"));

    options.AddPolicy("ManagerOrAbove", policy =>
        policy.RequireRole("Manager", "Admin", "SuperAdmin"));

    options.AddPolicy("UserOrAbove", policy =>
        policy.RequireRole("User", "Manager", "Admin", "SuperAdmin"));

    options.AddPolicy("DomainUser", policy =>
        policy.RequireAssertion(context =>
            context.User.Identity?.Name?.StartsWith("NIKONOA\\", StringComparison.OrdinalIgnoreCase) == true));
});

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "iLearn API", Version = "v1" });
});
builder.Services.AddHttpContextAccessor();

// ── Clean Architecture: Register layers via extension methods ──
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins(
                    "https://localhost:7270",
                    "https://localhost:7078"

                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});
}


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "iLearn API V1");
    });

   
}
app.UseCors("AllowSpecificOrigin");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
