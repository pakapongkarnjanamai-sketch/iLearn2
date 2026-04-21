namespace iLearn.API.Extensions
{
    /// <summary>
    /// Authorization composition for the API host. Defines the named policies
    /// referenced from controllers via <c>[Authorize(Policy = "...")]</c>.
    ///
    /// The Windows domain prefix used by the <c>DomainUser</c> policy is read
    /// from <c>Authentication:DomainPrefix</c> in configuration (defaults to
    /// <c>NIKONOA\</c> for backwards compatibility).
    /// </summary>
    public static class AuthorizationExtensions
    {
        public const string DefaultDomainPrefix = "NIKONOA\\";

        public static IServiceCollection AddApiAuthorization(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var domainPrefix = configuration["Authentication:DomainPrefix"] ?? DefaultDomainPrefix;

            services.AddAuthorization(options =>
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
                        context.User.Identity?.Name?.StartsWith(domainPrefix, StringComparison.OrdinalIgnoreCase) == true));
            });

            return services;
        }
    }
}
