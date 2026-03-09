using iLearn.User.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace iLearn.User.Middleware
{
    public class ApiUserSyncMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiUserSyncMiddleware> _logger;
        private readonly IMemoryCache _cache;

        public ApiUserSyncMiddleware(RequestDelegate next, ILogger<ApiUserSyncMiddleware> logger, IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, IApiUserService apiUserService)
        {
            if (ShouldSkipMiddleware(context))
            {
                await _next(context);
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true)
            {
                var windowsIdentity = context.User.Identity.Name;
                if (!string.IsNullOrEmpty(windowsIdentity) &&
                    windowsIdentity.StartsWith("NIKONOA\\", StringComparison.OrdinalIgnoreCase))
                {
                    var cacheKey = $"user_data_{windowsIdentity}";
                    var hasRoleClaims = context.User.Claims.Any(c => c.Type == ClaimTypes.Role);

                    if (!hasRoleClaims || !_cache.TryGetValue(cacheKey, out var _))
                    {
                        _logger.LogInformation("Syncing user data for: {WindowsIdentity}", windowsIdentity);

                        try
                        {
                            var userResponse = await apiUserService.GetOrCreateUserAsync(windowsIdentity);
                            if (userResponse.Success && userResponse.Data != null)
                            {
                                var user = userResponse.Data;

                                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, windowsIdentity),
                                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                    new Claim("UserId", user.Id.ToString()),
                                    new Claim("FullName", user.FullName),
                                    new Claim("Email", user.Email)
                                };

                                foreach (var role in user.Roles)
                                    claims.Add(new Claim(ClaimTypes.Role, role.Name));

                                var claimsIdentity = new ClaimsIdentity(claims, context.User.Identity.AuthenticationType);
                                context.User = new ClaimsPrincipal(claimsIdentity);

                                _cache.Set(cacheKey, user, TimeSpan.FromMinutes(10));

                                _logger.LogInformation("User {WindowsIdentity} synced with {RoleCount} roles",
                                    windowsIdentity, user.Roles.Count);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to sync user data for: {WindowsIdentity}", windowsIdentity);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error syncing Windows user via API: {WindowsIdentity}", windowsIdentity);
                        }
                    }
                }
            }

            await _next(context);
        }

        private static bool ShouldSkipMiddleware(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();

            if (path != null && (
                path.StartsWith("/_framework/") ||
                path.StartsWith("/css/") ||
                path.StartsWith("/js/") ||
                path.StartsWith("/lib/") ||
                path.StartsWith("/images/") ||
                path.StartsWith("/favicon.ico") ||
                path.EndsWith(".css") ||
                path.EndsWith(".js") ||
                path.EndsWith(".png") ||
                path.EndsWith(".jpg") ||
                path.EndsWith(".jpeg") ||
                path.EndsWith(".gif") ||
                path.EndsWith(".svg") ||
                path.EndsWith(".ico") ||
                path.EndsWith(".woff") ||
                path.EndsWith(".woff2") ||
                path.EndsWith(".ttf") ||
                path.EndsWith(".eot")))
            {
                return true;
            }

            return false;
        }
    }
}
