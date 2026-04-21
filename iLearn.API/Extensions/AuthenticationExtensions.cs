using Microsoft.AspNetCore.Authentication.Negotiate;

namespace iLearn.API.Extensions
{
    /// <summary>
    /// Authentication composition for the API host. Uses Windows / Negotiate
    /// authentication via the Microsoft.AspNetCore.Authentication.Negotiate package.
    /// </summary>
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                    .AddNegotiate();

            return services;
        }
    }
}
