using iLearn.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace iLearn.Admin.Middleware
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
                    var cacheKey = $"user_claims_{windowsIdentity}";
                    var forceRefresh = context.Request.Query.ContainsKey("_refresh");

                    if (forceRefresh)
                    {
                        _cache.Remove(cacheKey);
                    }

                    if (!forceRefresh &&
                        _cache.TryGetValue(cacheKey, out List<Claim>? cachedClaims) &&
                        cachedClaims != null)
                    {
                        var identity = new ClaimsIdentity(cachedClaims, "iLearnAuth");
                        context.User.AddIdentity(identity);

                        await _next(context);
                        return;
                    }

                    _logger.LogInformation("Syncing user claims from API for: {WindowsIdentity}", windowsIdentity);

                    try
                    {
                        var userResponse = await apiUserService.GetOrCreateUserAsync(windowsIdentity, forceRefresh);
                        if (userResponse.Success && userResponse.Data != null)
                        {
                            var user = userResponse.Data;

                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, windowsIdentity),
                                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                                new Claim("UserId", user.Id.ToString()),
                                new Claim("FullName", user.FullName ?? ""),
                                new Claim("Email", user.Email ?? "")
                            };

                            foreach (var role in user.Roles ?? [])
                            {
                                var roleValue = role.RoleType?.ToString();
                                if (!string.IsNullOrWhiteSpace(roleValue))
                                {
                                    claims.Add(new Claim(ClaimTypes.Role, roleValue));
                                }
                            }

                            var primaryDivisionId = user.Roles?
                                .Where(r => r.DivisionId.HasValue)
                                .Select(r => r.DivisionId!.Value)
                                .FirstOrDefault() ?? 0;

                            if (primaryDivisionId > 0)
                            {
                                claims.Add(new Claim("DivisionId", primaryDivisionId.ToString()));
                            }

                            _cache.Set(cacheKey, claims, TimeSpan.FromMinutes(10));

                            var newIdentity = new ClaimsIdentity(claims, "iLearnAuth");
                            context.User.AddIdentity(newIdentity);

                            _logger.LogInformation(
                                "User {Identity} synced: {RoleCount} role(s) [{Roles}], DivisionId={DivisionId}",
                                windowsIdentity,
                                user.Roles?.Count ?? 0,
                                string.Join(", ", user.Roles?.Select(r => r.Name) ?? []),
                                primaryDivisionId > 0 ? primaryDivisionId.ToString() : "N/A");
                        }
                        else
                        {
                            _logger.LogWarning(
                                "API sync failed for: {WindowsIdentity} - {Message}",
                                windowsIdentity,
                                userResponse.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing Windows user via API: {WindowsIdentity}", windowsIdentity);
                    }
                }
            }

            await _next(context);
        }

        private static bool ShouldSkipMiddleware(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (path == null) return false;

            var staticFilePaths = new[] { "/_framework/", "/css/", "/js/", "/lib/", "/images/", "/favicon.ico" };
            var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff", ".woff2", ".ttf", ".eot" };

            return staticFilePaths.Any(p => path.StartsWith(p)) ||
                   staticExtensions.Any(e => path.EndsWith(e));
        }
    }
}
