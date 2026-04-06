using iLearn.Application.Interfaces.Services;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace iLearn.Admin.Middleware
{
    public class ApiUserSyncMiddleware
    {
        private const string DomainPrefix = "NIKONOA\\";
        private const string ClaimsCachePrefix = "user_claims_";
        private const string IdentityAuthenticationType = "iLearnAuth";
        private static readonly TimeSpan ClaimsCacheDuration = TimeSpan.FromMinutes(10);
        private static readonly string[] StaticFilePaths = ["/_framework/", "/css/", "/js/", "/lib/", "/images/", "/favicon.ico"];
        private static readonly string[] StaticExtensions = [".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff", ".woff2", ".ttf", ".eot"];

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

            var windowsIdentity = GetWindowsIdentity(context.User);
            if (windowsIdentity == null)
            {
                await _next(context);
                return;
            }

            var forceRefresh = context.Request.Query.ContainsKey("_refresh");
            var cacheKey = GetCacheKey(windowsIdentity);

            if (forceRefresh)
            {
                _cache.Remove(cacheKey);
            }

            if (TryApplyCachedClaims(context, cacheKey, forceRefresh))
            {
                await _next(context);
                return;
            }

            await SyncClaimsAsync(context, apiUserService, windowsIdentity, cacheKey, forceRefresh);
            await _next(context);
        }

        private async Task SyncClaimsAsync(
            HttpContext context,
            IApiUserService apiUserService,
            string windowsIdentity,
            string cacheKey,
            bool forceRefresh)
        {
            _logger.LogInformation("Syncing user claims from API for: {WindowsIdentity}", windowsIdentity);

            try
            {
                var userResponse = await apiUserService.GetOrCreateUserAsync(windowsIdentity, forceRefresh);
                if (!userResponse.Success || userResponse.Data == null)
                {
                    _logger.LogWarning(
                        "API sync failed for: {WindowsIdentity} - {Message}",
                        windowsIdentity,
                        userResponse.Message);
                    return;
                }

                var user = userResponse.Data;
                var claims = BuildClaims(windowsIdentity, user);
                var primaryDivisionId = GetPrimaryDivisionId(user);

                _cache.Set(cacheKey, claims, ClaimsCacheDuration);
                context.User.AddIdentity(new ClaimsIdentity(claims, IdentityAuthenticationType));

                _logger.LogInformation(
                    "User {Identity} synced: {RoleCount} role(s) [{Roles}], DivisionId={DivisionId}",
                    windowsIdentity,
                    user.Roles?.Count ?? 0,
                    string.Join(", ", user.Roles?.Select(r => r.Name) ?? []),
                    primaryDivisionId?.ToString() ?? "N/A");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Windows user via API: {WindowsIdentity}", windowsIdentity);
            }
        }

        private bool TryApplyCachedClaims(HttpContext context, string cacheKey, bool forceRefresh)
        {
            if (forceRefresh || !_cache.TryGetValue(cacheKey, out List<Claim>? cachedClaims) || cachedClaims == null)
            {
                return false;
            }

            context.User.AddIdentity(new ClaimsIdentity(cachedClaims, IdentityAuthenticationType));
            return true;
        }

        private static string? GetWindowsIdentity(ClaimsPrincipal user)
        {
            var windowsIdentity = user.Identity?.IsAuthenticated == true ? user.Identity.Name : null;
            if (string.IsNullOrWhiteSpace(windowsIdentity) ||
                !windowsIdentity.StartsWith(DomainPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return windowsIdentity;
        }

        private static List<Claim> BuildClaims(string windowsIdentity, UserDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, windowsIdentity),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("FullName", user.FullName ?? string.Empty),
                new Claim("Email", user.Email ?? string.Empty)
            };

            foreach (var role in user.Roles ?? [])
            {
                var roleValue = role.RoleType?.ToString();
                if (!string.IsNullOrWhiteSpace(roleValue))
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleValue));
                }
            }

            var primaryDivisionId = GetPrimaryDivisionId(user);
            if (primaryDivisionId.HasValue)
            {
                claims.Add(new Claim("DivisionId", primaryDivisionId.Value.ToString()));
            }

            return claims;
        }

        private static int? GetPrimaryDivisionId(UserDto user)
        {
            var primaryDivisionId = user.Roles?
                .Where(r => r.DivisionId.HasValue)
                .Select(r => (int?)r.DivisionId!.Value)
                .FirstOrDefault();

            return primaryDivisionId > 0 ? primaryDivisionId : null;
        }

        private static string GetCacheKey(string windowsIdentity) => $"{ClaimsCachePrefix}{windowsIdentity}";

        private static bool ShouldSkipMiddleware(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (path == null) return false;

            return StaticFilePaths.Any(p => path.StartsWith(p)) ||
                   StaticExtensions.Any(e => path.EndsWith(e));
        }
    }
}
