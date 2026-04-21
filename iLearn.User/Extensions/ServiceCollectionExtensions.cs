using iLearn.User.Interfaces;
using iLearn.User.Services;

namespace iLearn.User.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUserServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();

            var apiBaseUrl = configuration["ApiSettings:BaseUrl"]
                ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");

            services.AddHttpClient("iLearnAPI", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            services.AddScoped<IApiUserService, ApiUserService>();

            return services;
        }
    }
}
