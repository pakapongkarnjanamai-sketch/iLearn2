using iLearn.Application.Common;
using iLearn.User.Interfaces;
using iLearn.User.Services;

namespace iLearn.User.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUserServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            services.Configure<LearnerProxyAuthOptions>(
                configuration.GetSection(LearnerProxyAuthOptions.SectionName));
            services.Configure<FileSettings>(
                configuration.GetSection(nameof(FileSettings)));

            var apiBaseUrl = configuration["ApiSettings:BaseUrl"]
                ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");

            services.AddHttpClient("iLearnAPI", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseDefaultCredentials = true
            });

            services.AddScoped<IApiUserService, ApiUserService>();

            return services;
        }
    }
}
