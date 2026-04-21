using iLearn.API.Hubs;
using iLearn.API.Services;
using iLearn.Application.Interfaces.Services;

namespace iLearn.API.Extensions
{
    /// <summary>
    /// Presentation-layer composition: controllers (with JSON options),
    /// SignalR hubs, and presentation-only services such as
    /// <see cref="SignalRAdminActivityNotifier"/>.
    /// </summary>
    public static class PresentationExtensions
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                // Some grid endpoints still return EF entity graphs (anonymous projections
                // included). Until DTO boundary cleanup is complete (see refactor plan
                // Phase 4), keep IgnoreCycles to avoid 500s on circular navigation properties.
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

            services.AddSignalR();
            services.AddSingleton<IAdminActivityRealtimeNotifier, SignalRAdminActivityNotifier>();

            return services;
        }
    }
}
