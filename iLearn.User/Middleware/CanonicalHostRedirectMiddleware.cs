using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace iLearn.User.Middleware
{
    public static class CanonicalHostRedirectHelper
    {
        public static bool TryGetCanonicalRedirect(
            string? hostUrlConfig,
            string requestMethod,
            string requestHost,
            string pathBase,
            string path,
            string queryString,
            out string? redirectUrl)
        {
            redirectUrl = null;

            if (!HttpMethods.IsGet(requestMethod) && !HttpMethods.IsHead(requestMethod))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(hostUrlConfig) ||
                !Uri.TryCreate(hostUrlConfig, UriKind.Absolute, out var canonicalUri))
            {
                return false;
            }

            if (canonicalUri.Scheme != Uri.UriSchemeHttp && canonicalUri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            var canonicalHost = canonicalUri.Host;
            if (IsLocalhost(canonicalHost))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(requestHost) || IsLocalhost(requestHost))
            {
                return false;
            }

            if (string.Equals(requestHost, canonicalHost, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var scheme = canonicalUri.Scheme;
            var authority = canonicalUri.Authority;

            var normalizedPathBase = string.IsNullOrEmpty(pathBase)
                ? string.Empty
                : (pathBase.StartsWith('/') ? pathBase : "/" + pathBase);

            var normalizedPath = string.IsNullOrEmpty(path)
                ? string.Empty
                : (path.StartsWith('/') ? path : "/" + path);

            redirectUrl = $"{scheme}://{authority}{normalizedPathBase}{normalizedPath}{queryString}";
            return true;
        }

        public static bool IsLocalhost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return false;

            var trimmedHost = host.Trim('[', ']').Trim();
            if (string.Equals(trimmedHost, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            if (IPAddress.TryParse(trimmedHost, out var ip))
            {
                return IPAddress.IsLoopback(ip);
            }

            return false;
        }
    }

    public class CanonicalHostRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string? _hostUrlConfig;

        public CanonicalHostRedirectMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _hostUrlConfig = configuration?["FileSettings:HostUrl"];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            if (CanonicalHostRedirectHelper.TryGetCanonicalRedirect(
                    _hostUrlConfig,
                    request.Method,
                    request.Host.Host,
                    request.PathBase.Value ?? string.Empty,
                    request.Path.Value ?? string.Empty,
                    request.QueryString.Value ?? string.Empty,
                    out var redirectUrl) && !string.IsNullOrEmpty(redirectUrl))
            {
                context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
                context.Response.Headers.Location = redirectUrl;
                return;
            }

            await _next(context);
        }
    }

    public static class CanonicalHostRedirectMiddlewareExtensions
    {
        public static IApplicationBuilder UseCanonicalHostRedirect(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CanonicalHostRedirectMiddleware>();
        }
    }
}
