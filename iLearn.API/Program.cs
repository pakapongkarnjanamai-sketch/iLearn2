using iLearn.Application.Interfaces;
using iLearn.Infrastructure.Persistence;
using iLearn.Infrastructure.Repositories;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer; // Add this using directive

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// --- 1. Database Connection ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 2. Register Repositories ---
// ลงทะเบียน Generic แบบนี้เพื่อให้ใช้ได้กับทุก Entity
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// ลงทะเบียน Repository เฉพาะทาง
builder.Services.AddScoped<ICourseRepository, CourseRepository>();


builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
