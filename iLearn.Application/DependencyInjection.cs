using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace iLearn.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Application Services
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<ICourseVersionService, CourseVersionService>();
            services.AddScoped<ICourseAssignmentService, CourseAssignmentService>();
            services.AddScoped<IAssignmentBatchService, AssignmentBatchService>();
            services.AddScoped<IAssignmentDashboardService, AssignmentDashboardService>();
            services.AddScoped<ILearnerGroupService, LearnerGroupService>();
            services.AddScoped<ILearnerGroupCategoryService, LearnerGroupCategoryService>();

            // Lazy<T> support to break circular dependencies
            services.AddTransient(typeof(Lazy<>), typeof(LazyServiceFactory<>));

            return services;
        }
    }

    internal sealed class LazyServiceFactory<T> : Lazy<T> where T : class
    {
        public LazyServiceFactory(IServiceProvider serviceProvider)
            : base(serviceProvider.GetRequiredService<T>) { }
    }
}
