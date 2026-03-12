using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SC = System.Security.Claims;

namespace iLearn.API.Middleware
{
    /// <summary>
    /// Middleware ?????? iLearn.API — ????? Claims ??? Windows Auth token
    /// ??? resolve DivisionId + Roles ??? DB ??? NID ??? Windows user
    /// ???????? ICurrentUserService.DivisionId ?????????? API controllers
    /// </summary>
    public class ApiClaimsEnrichMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiClaimsEnrichMiddleware> _logger;
        private readonly IMemoryCache _cache;

        // Cache TTL ??????? Admin middleware ????????????????????
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

        public ApiClaimsEnrichMiddleware(
            RequestDelegate next,
            ILogger<ApiClaimsEnrichMiddleware> logger,
            IMemoryCache cache)
        {
            _next   = next;
            _logger = logger;
            _cache  = cache;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IGenericRepository<User> userRepo)
        {
            // ?? Skip static / non-authenticated ??
            if (!context.User.Identity?.IsAuthenticated == true ||
                ShouldSkip(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var windowsName = context.User.Identity?.Name ?? "";
            if (string.IsNullOrEmpty(windowsName) ||
                !windowsName.StartsWith("NIKONOA\\", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var cacheKey     = $"api_claims_{windowsName}";
            var forceRefresh = context.Request.Query.ContainsKey("_refresh");

            // ?? Cache hit ? inject ????????????? ??
            if (!forceRefresh && _cache.TryGetValue(cacheKey, out SC.ClaimsPrincipal? cached) && cached != null)
            {
                context.User = cached;
                await _next(context);
                return;
            }

            // ?? Cache miss ? query DB ??
            try
            {
                var nid = windowsName.Split('\\')[1];

                var userRecord = await userRepo.GetQuery()
                    .Where(u => u.Nid == nid)
                    .Select(u => new
                    {
                        u.Id,
                        Roles = u.UserRoles.Select(ur => new RoleDto
                        {
                            Id         = ur.Role.Id,
                            Name       = ur.Role.Name,
                            DivisionId = ur.Role.DivisionId
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (userRecord == null)
                {
                    _logger.LogWarning("ApiClaimsEnrich: user not found in DB for {Identity}", windowsName);
                    await _next(context);
                    return;
                }

                var claims = new List<SC.Claim>
                {
                    new SC.Claim(SC.ClaimTypes.Name, windowsName),
                    new SC.Claim(SC.ClaimTypes.NameIdentifier, userRecord.Id.ToString()),
                    new SC.Claim("UserId", userRecord.Id.ToString())
                };

                // Role claims
                foreach (var role in userRecord.Roles)
                    claims.Add(new SC.Claim(SC.ClaimTypes.Role, role.Name));

                // DivisionId claim ??? Role ???????????
                var primaryDivisionId = userRecord.Roles
                    .Where(r => r.DivisionId.HasValue)
                    .Select(r => r.DivisionId!.Value)
                    .FirstOrDefault();

                if (primaryDivisionId > 0)
                    claims.Add(new SC.Claim("DivisionId", primaryDivisionId.ToString()));

                var identity  = new SC.ClaimsIdentity(claims, context.User.Identity!.AuthenticationType);
                var principal = new SC.ClaimsPrincipal(identity);

                context.User = principal;
                _cache.Set(cacheKey, principal, CacheTtl);

                _logger.LogInformation(
                    "ApiClaimsEnrich: enriched {Identity} with {RoleCount} role(s), DivisionId={DivId}",
                    windowsName,
                    userRecord.Roles.Count,
                    primaryDivisionId > 0 ? primaryDivisionId.ToString() : "—");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApiClaimsEnrich: error enriching claims for {Identity}", windowsName);
            }

            await _next(context);
        }

        private static bool ShouldSkip(PathString path)
        {
            var p = path.Value?.ToLowerInvariant() ?? "";
            return p.StartsWith("/swagger") || p.StartsWith("/health");
        }
    }
}
