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
            services.AddSingleton<IMaintenanceStatusService, MaintenanceStatusService>();

            // ── External HTTP Services ──
            services.AddHttpClient<IStudentApiService, StudentApiService>();

            return services;
        }
    }
}
