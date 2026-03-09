using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace iLearn.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // ?? Application Services ??
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<ICourseVersionService, CourseVersionService>();
            services.AddScoped<ICourseAssignmentService, CourseAssignmentService>();
            services.AddScoped<IAssignmentDashboardService, AssignmentDashboardService>();
            services.AddScoped<IStudentGroupService, StudentGroupService>();

            return services;
        }
    }
}
