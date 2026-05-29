using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Entities;
using iLearn.API.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SC = System.Security.Claims;

namespace iLearn.API.Middleware
{
    /// <summary>
    /// Enriches the Windows-authenticated <see cref="System.Security.Claims.ClaimsPrincipal"/>
    /// with iLearn-specific claims (UserId, Roles, DivisionId) resolved from the database
    /// using the NID portion of the Windows account name. Results are cached per identity
    /// so subsequent requests do not re-query the user/role tables.
    ///
    /// The enriched principal exposes <c>DivisionId</c> via <see cref="iLearn.Application.Interfaces.Services.ICurrentUserService"/>,
    /// which API controllers rely on for division-level data isolation.
    /// </summary>
    public class ApiClaimsEnrichMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiClaimsEnrichMiddleware> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _domainPrefix;

        // Cache TTL kept aligned with the Admin-side middleware to avoid
        // divergent permission decisions between API and Admin layers.
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

        public ApiClaimsEnrichMiddleware(
            RequestDelegate next,
            ILogger<ApiClaimsEnrichMiddleware> logger,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _next   = next;
            _logger = logger;
            _cache  = cache;
            _domainPrefix = configuration["Authentication:DomainPrefix"]
                ?? AuthorizationExtensions.DefaultDomainPrefix;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IGenericRepository<User> userRepo,
            ILearnerApiService learnerApiService)
        {
            // Skip swagger/health/static and any unauthenticated request.
            if (context.User.Identity?.IsAuthenticated != true ||
                ShouldSkip(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var windowsName = context.User.Identity?.Name ?? "";
            if (string.IsNullOrEmpty(windowsName) ||
                !windowsName.StartsWith(_domainPrefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var cacheKey = $"api_claims_{windowsName}";

            // The "?_refresh" query toggle is intentionally restricted to administrators
            // so an arbitrary authenticated client cannot force repeated DB look-ups.
            // Non-admin callers always read the cached principal.
            var forceRefresh = context.Request.Query.ContainsKey("_refresh")
                && IsAdminPrincipal(context.User);

            // Cache hit: replace the principal and short-circuit.
            if (!forceRefresh && _cache.TryGetValue(cacheKey, out SC.ClaimsPrincipal? cached) && cached != null)
            {
                context.User = cached;
                await _next(context);
                return;
            }

            // Cache miss: build claims from the database.
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
                            Id           = ur.Role.Id,
                            Name         = ur.Role.Name,
                            RoleType     = ur.Role.RoleType,
                            DivisionId   = ur.Role.DivisionId,
                            DivisionName = ur.Role.Division != null ? ur.Role.Division.Name : null
                        }).ToList()
                    })
                    .FirstOrDefaultAsync(context.RequestAborted);

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
                {
                    var roleValue = role.RoleType?.ToString();
                    if (!string.IsNullOrWhiteSpace(roleValue))
                        claims.Add(new SC.Claim(SC.ClaimTypes.Role, roleValue));
                }

                // DivisionId claim — taken from the first role that carries one.
                var primaryDivisionId = userRecord.Roles
                    .Where(r => r.DivisionId.HasValue)
                    .Select(r => r.DivisionId!.Value)
                    .FirstOrDefault();

                var primaryDivisionName = userRecord.Roles
                    .Where(r => r.DivisionId.HasValue && !string.IsNullOrWhiteSpace(r.DivisionName))
                    .Select(r => r.DivisionName)
                    .FirstOrDefault();

                if (primaryDivisionId > 0)
                    claims.Add(new SC.Claim("DivisionId", primaryDivisionId.ToString()));

                if (!string.IsNullOrWhiteSpace(primaryDivisionName))
                    claims.Add(new SC.Claim("Division", primaryDivisionName));

                // DisplayName — resolve from external employee directory (best effort)
                try
                {
                    var employees = await learnerApiService.GetEmployeesByNidsAsync(new[] { nid });
                    if (employees.TryGetValue(nid, out var emp) && !string.IsNullOrWhiteSpace(emp.FullName))
                        claims.Add(new SC.Claim("DisplayName", emp.FullName));
                }
                catch (Exception empEx)
                {
                    _logger.LogDebug(empEx, "ApiClaimsEnrich: could not resolve DisplayName for {Nid}", nid);
                }

                var identity  = new SC.ClaimsIdentity(claims, context.User.Identity!.AuthenticationType);
                var principal = new SC.ClaimsPrincipal(identity);

                context.User = principal;
                _cache.Set(cacheKey, principal, CacheTtl);

                _logger.LogInformation(
                    "ApiClaimsEnrich: enriched {Identity} with {RoleCount} role(s), DivisionId={DivId}, Division={Division}",
                    windowsName,
                    userRecord.Roles.Count,
                    primaryDivisionId > 0 ? primaryDivisionId.ToString() : "(none)",
                    primaryDivisionName ?? "(none)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApiClaimsEnrich: error enriching claims for {Identity}", windowsName);
            }

            await _next(context);
        }

        private static bool IsAdminPrincipal(SC.ClaimsPrincipal user) =>
            user.IsInRole("Admin") || user.IsInRole("SuperAdmin");

        private static bool ShouldSkip(PathString path)
        {
            var p = path.Value?.ToLowerInvariant() ?? "";
            return p.StartsWith("/swagger") || p.StartsWith("/health");
        }
    }
}
