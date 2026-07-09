using iLearn.Application.Common;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Infrastructure.Persistence;
using iLearn.Infrastructure.Repositories;
using iLearn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace iLearn.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── Database ──
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            // ── Configuration ──
            services.Configure<FileSettings>(
                configuration.GetSection("FileSettings"));
            services.Configure<EmployeeServiceSettings>(
                configuration.GetSection("EmployeeServiceSettings"));


            // ── Unit of Work ──
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ── Repositories ──
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<ICourseRepository, CourseRepository>();

            // ── Infrastructure Services ──
            services.AddTransient<IDateTime, DateTimeService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IAdminActivityService, AdminActivityService>();
            services.AddScoped<IAssignmentNoGenerator, AssignmentNoGenerator>();
            services.AddScoped<IScormService, ScormService>();
            services.AddScoped<IScormRuntimeStateService, ScormRuntimeStateService>();
            services.AddSingleton<IMaintenanceStatusService, MaintenanceStatusService>();

            // ── External HTTP Services ──
            var employeeSettings = configuration.GetSection("EmployeeServiceSettings");
            var provider = employeeSettings["Provider"];

            if (string.Equals(provider, "EmployeeHub", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[iLearn API Startup] Employee Service Provider is configured to 'EmployeeHub'. Registering EmployeeHubClient and EmployeeHubLearnerApiService.");

                services.AddHttpClient<EmployeeHubClient>((serviceProvider, client) =>
                {
                    var baseUrl = employeeSettings["EmployeeHubBaseUrl"] ?? string.Empty;
                    if (!baseUrl.EndsWith("/"))
                    {
                        baseUrl += "/";
                    }
                    client.BaseAddress = new Uri(baseUrl);
                });

                services.AddScoped<ILearnerApiService, EmployeeHubLearnerApiService>();
            }
            else
            {
                Console.WriteLine("[iLearn API Startup] Employee Service Provider is configured to 'Legacy'. Registering LearnerApiService.");

                services.AddHttpClient<ILearnerApiService, LearnerApiService>();
            }

            return services;
        }
    }
}
