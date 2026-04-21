namespace iLearn.API.Extensions
{
    /// <summary>
    /// CORS composition for the API host. Allowed origins are read from
    /// <c>Cors:AllowedOrigins</c> in configuration (array of strings).
    /// CORS is only enabled in Development; production hosts are expected
    /// to be on the same origin.
    /// </summary>
    public static class CorsExtensions
    {
        public const string DevelopmentPolicyName = "AllowSpecificOrigin";

        // Fallback origins used when configuration is missing — preserves the
        // historical hard-coded list previously embedded in Program.cs.
        private static readonly string[] DefaultDevelopmentOrigins =
        {
            "https://localhost:7270",
            "https://localhost:7078",
            "http://localhost:5126",
            "http://localhost:5182"
        };

        public static IServiceCollection AddApiCors(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger? logger = null)
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (origins is null || origins.Length == 0)
            {
                logger?.LogWarning(
                    "Cors:AllowedOrigins is not configured; falling back to built-in development origins. " +
                    "Set Cors:AllowedOrigins in configuration for non-default deployments.");
                origins = DefaultDevelopmentOrigins;
            }

            services.AddCors(options =>
            {
                options.AddPolicy(DevelopmentPolicyName, policyBuilder =>
                {
                    policyBuilder
                        .WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            return services;
        }
    }
}
